using LogiMarket.Application.Contratos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Tests.Integracion;

public sealed class RegresionesCobroRevisionTests
{
    [Fact]
    public async Task DiarioDeVersionAnteriorConAceptacionSinPedido_ImpideOtroCargoHastaConciliar()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.ValidadoAsync();
        // Formato anterior: el proveedor acepto sin guardar la identidad del pedido.
        e.Diario.Aprobar(59, "clave-version-anterior", "LEGADO");
        await using var r = new Escenario(e.Config.Ruta); await r.InicializarAsync();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => r.Facade.ConfirmarAsync(id, "nueva-clave"));
        Assert.Contains("conciliacion", error.Message); Assert.Single(r.Diario.Leer()); Assert.Empty((await r.Consultas.CargarAsync()).Pagos);
    }

    [Fact]
    public async Task DiarioAnteriorConPagosYaConciliados_PermiteNuevasVentasYRecuperaElPago()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.PagadoAsync();
        var original = (await e.Consultas.CargarAsync()).Pagos.Single();
        var ruta = Path.Combine(e.Config.Directorio, "cobros-simulados.json");
        var anteriores = e.Diario.Leer().Select(x => new { x.Clave, x.Monto, x.Referencia, x.Proveedor });
        await File.WriteAllTextAsync(ruta, System.Text.Json.JsonSerializer.Serialize(anteriores));
        await using var r = new Escenario(e.Config.Ruta); await r.InicializarAsync();
        Assert.Equal(original, await r.Facade.ConfirmarAsync(id, "alias-del-pago-anterior"));
        var nuevo = await r.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P05", 1));
        await r.Facade.ConfirmarAsync(nuevo, "venta-nueva"); Assert.Equal(2, r.Diario.Leer().Count);
        Assert.Equal(2, (await r.Consultas.CargarAsync()).Pagos.Count);
    }

    [Theory]
    [InlineData("LEGADO", "ALTERNATIVO")]
    [InlineData("ALTERNATIVO", "LEGADO")]
    public async Task RollbackOtraClaveReinicioYOtroProveedor_RecuperaUnSoloCobro(string primero, string segundo)
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.ValidadoAsync();
        e.Demo.Proveedor = primero; e.Demo.FallarPersistencia = true;
        await Assert.ThrowsAsync<IOException>(() => e.Facade.ConfirmarAsync(id, "intento-A"));
        var aceptado = Assert.Single(e.Diario.Leer()); Assert.Empty((await e.Consultas.CargarAsync()).Pagos);
        await using var reinicio = new Escenario(e.Config.Ruta); await reinicio.InicializarAsync(); reinicio.Demo.Proveedor = segundo;
        var pago = await reinicio.Invocador.EjecutarAsync(new ConfirmarPedidoCommand(reinicio.Facade, id, "intento-B"));
        Assert.Equal(aceptado.Referencia, pago.Referencia); Assert.Equal(primero, pago.Proveedor);
        Assert.Equal(pago, await reinicio.Facade.ConfirmarAsync(id, "intento-A"));
        await reinicio.Outbox.ProcesarAsync(); await reinicio.Outbox.ProcesarAsync();
        var d = await reinicio.Consultas.CargarAsync(); Assert.Single(d.Pagos); Assert.Equal(3, d.Movimientos.Count);
        Assert.Single(d.Outbox); Assert.Single(d.Avisos); Assert.Equal(1, reinicio.Metricas.Pagos);
        Assert.Single(reinicio.Diario.Leer()); Assert.Single(reinicio.Log.Leer().Where(x => x.Tipo == "PASARELA"));
        await reinicio.EvidenciaAsync("R01-" + primero, "Un cobro tras rollback, nueva clave, reinicio y cambio de proveedor; un pago/movimiento por SKU/evento/aviso", new { pago, Cobros = reinicio.Diario.Leer() });
    }

    [Fact]
    public async Task ClaveDeIntentoRevertido_NoPuedePasarAOtroPedido()
    {
        await using var e = await Escenario.CrearAsync(); var primero = await e.ValidadoAsync();
        var segundo = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P05", 1)); e.Demo.FallarPersistencia = true;
        await Assert.ThrowsAsync<IOException>(() => e.Facade.ConfirmarAsync(primero, "clave-reservada")); e.Demo.FallarPersistencia = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Facade.ConfirmarAsync(segundo, "clave-reservada"));
        Assert.Single(e.Diario.Leer()); Assert.Empty((await e.Consultas.CargarAsync()).Pagos);
        await e.Facade.ConfirmarAsync(primero, "otra-clave"); Assert.Single(e.Diario.Leer());
    }

    [Fact]
    public async Task ClavesAlternativasDelPagoConfirmado_ConservanSuPropietario()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.PagadoAsync();
        var original = (await e.Consultas.CargarAsync()).Pagos.Single();
        Assert.Equal(original, await e.Facade.ConfirmarAsync(id, "alias-nuevo"));
        var otro = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P05", 1));
        await using var reinicio = new Escenario(e.Config.Ruta); await reinicio.InicializarAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => reinicio.Facade.ConfirmarAsync(otro, "alias-nuevo"));
        Assert.Equal(original, await reinicio.Facade.ConfirmarAsync(id, "alias-nuevo")); Assert.Single(reinicio.Diario.Leer());
    }

    [Fact]
    public async Task ReintentosConcurrentesConClavesDistintas_DevuelvenElMismoPago()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.ValidadoAsync(); e.Demo.FallarPersistencia = true;
        await Assert.ThrowsAsync<IOException>(() => e.Facade.ConfirmarAsync(id, "fallo"));
        await using var a = new Escenario(e.Config.Ruta); await a.ComoAsync(Rol.VENTAS);
        await using var b = new Escenario(e.Config.Ruta); await b.ComoAsync(Rol.VENTAS); b.Demo.Proveedor = "ALTERNATIVO";
        using var inicio = new ManualResetEventSlim(false);
        var uno = Task.Run(async () => { inicio.Wait(); return await a.Facade.ConfirmarAsync(id, "competidor-A"); });
        var dos = Task.Run(async () => { inicio.Wait(); return await b.Facade.ConfirmarAsync(id, "competidor-B"); });
        inicio.Set(); var pagos = await Task.WhenAll(uno, dos); Assert.Equal(pagos[0], pagos[1]);
        Assert.Single(a.Diario.Leer()); var d = await a.Consultas.CargarAsync(); Assert.Single(d.Pagos); Assert.Equal(3, d.Movimientos.Count); Assert.Single(d.Outbox);
    }

    [Fact]
    public async Task FalloDePersistenciaDelCobro_NoImpideRegistrarElComandoNiSuError()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.ValidadoAsync(); e.Demo.FallarPersistencia = true;
        await Assert.ThrowsAsync<IOException>(() => e.Invocador.EjecutarAsync(new ConfirmarPedidoCommand(e.Facade, id, "boton-A")));
        Assert.Single(e.Diario.Leer()); var d = await e.Consultas.CargarAsync(); Assert.Empty(d.Pagos); Assert.Empty(d.Movimientos); Assert.Empty(d.Outbox);
        Assert.Contains(d.Auditoria, x => x.Tipo == "COMMAND" && x.Mensaje.Contains("ERROR"));
        e.Demo.FallarPersistencia = false;
        await e.Invocador.EjecutarAsync(new ConfirmarPedidoCommand(e.Facade, id, "boton-B")); Assert.Single(e.Diario.Leer());
    }

    [Fact]
    public async Task CancelarDespuesDeUnCobroAceptadoPendiente_ExigeConciliar()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.ValidadoAsync(); e.Demo.FallarPersistencia = true;
        await Assert.ThrowsAsync<IOException>(() => e.Facade.ConfirmarAsync(id, "pendiente")); e.Demo.FallarPersistencia = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Pedidos.CancelarAsync(id));
        await e.Facade.ConfirmarAsync(id, "recuperar"); Assert.Single(e.Diario.Leer());
    }
}
