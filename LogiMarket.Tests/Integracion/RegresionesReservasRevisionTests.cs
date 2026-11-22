using LogiMarket.Application.Pedidos;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Tests.Integracion;

public sealed class RegresionesReservasRevisionTests
{
    [Fact]
    public async Task ConsultaDeAceptaciones_EsperaElBloqueoBreveDeOtraInstancia()
    {
        await using var e = await Escenario.CrearAsync();
        using var bloqueo = new FileStream(Path.Combine(e.Config.Directorio, "cobros-simulados.json.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var inicio = new ManualResetEventSlim(false);
        var lectura = Task.Run(async () => { inicio.Set(); return await e.Diario.AceptadosAsync(); });
        inicio.Wait(); await Task.Delay(100); bloqueo.Dispose(); Assert.Empty(await lectura);
    }

    [Fact]
    public async Task EsperaDelDiario_RespetaCancelacionSinCrearAceptacion()
    {
        await using var e = await Escenario.CrearAsync();
        using var bloqueo = new FileStream(Path.Combine(e.Config.Directorio, "cobros-simulados.json.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var inicio = new ManualResetEventSlim(false); using var cancelar = new CancellationTokenSource();
        var lectura = Task.Run(async () => { inicio.Set(); return await e.Diario.AceptadosAsync(cancelar.Token); });
        inicio.Wait(); await Task.Delay(100); cancelar.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => lectura); bloqueo.Dispose(); Assert.Empty(e.Diario.Leer());
    }

    [Theory]
    [InlineData("LEGADO", "ALTERNATIVO")]
    [InlineData("ALTERNATIVO", "LEGADO")]
    public async Task AceptacionPendiente_ComprometeTodoHastaConsumirUnaVez(string inicial, string recuperacion)
    {
        await using var e = await Escenario.CrearAsync();
        var a = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 4));
        var b = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 4));
        e.Demo.Proveedor = inicial; e.Demo.FallarPersistencia = true;
        await Assert.ThrowsAsync<IOException>(() => e.Facade.ConfirmarAsync(a, "A-fallo"));
        await Assert.ThrowsAsync<IOException>(() => e.Facade.ConfirmarAsync(a, "A-otro-fallo"));
        var referencia = Assert.Single(e.Diario.Leer()).Referencia;
        var antes = await e.Consultas.CargarAsync(); Assert.Empty(antes.Pagos); Assert.Empty(antes.Movimientos); Assert.Empty(antes.Outbox);
        Assert.Equal(4, antes.Productos.Single(x => x.Sku == "P04").Stock);
        var existencias = antes.Existencias.Single(x => x.Sku == "P04"); Assert.Equal(4, existencias.Comprometido); Assert.Equal(0, existencias.Disponible);
        await e.EvidenciaAsync("R06-PENDIENTE-" + inicial, "Una aceptacion165 compromete P04x4 pese a dos rollback; stock fisico4 y operaciones locales0", new { a, b, referencia });
        e.Demo.FallarPersistencia = false;
        var rechazo = await Assert.ThrowsAsync<InvalidOperationException>(() => e.Facade.ConfirmarAsync(b, "B-sin-libre"));
        Assert.Contains("STOCK", rechazo.Message); Assert.Contains("disponible 0", rechazo.Message);
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Pedidos.CancelarAsync(a));
        Assert.Single(e.Diario.Leer());
        await using var r = new Escenario(e.Config.Ruta); await r.InicializarAsync(); r.Demo.Proveedor = recuperacion;
        var pago = await r.Invocador.EjecutarAsync(new ConfirmarPedidoCommand(r.Facade, a, "A-reinicio"));
        Assert.Equal(165, pago.Importe); Assert.Equal(referencia, pago.Referencia); Assert.Equal(inicial, pago.Proveedor);
        Assert.Equal(pago, await r.Facade.ConfirmarAsync(a, "A-fallo")); Assert.Equal(pago, await r.Facade.ConfirmarAsync(a, "A-tercera-clave"));
        r.Demo.FallarNotificacion = true; await r.Outbox.ProcesarAsync();
        Assert.Null((await r.Consultas.CargarAsync()).Outbox.Single().Procesado);
        await using var s = new Escenario(e.Config.Ruta); await s.InicializarAsync(); await s.Outbox.ProcesarAsync(); await s.Outbox.ProcesarAsync();
        var d = await s.Consultas.CargarAsync(); Assert.Single(d.Pagos); var movimiento = Assert.Single(d.Movimientos);
        Assert.Equal(a, movimiento.PedidoId); Assert.Equal(-4, movimiento.Cantidad); Assert.Equal("P04", movimiento.Sku);
        Assert.Equal(0, d.Productos.Single(x => x.Sku == "P04").Stock); Assert.All(d.Productos, x => Assert.True(x.Stock >= 0));
        Assert.All(d.Existencias, x => Assert.Equal(0, x.Comprometido));
        Assert.Single(s.Diario.Leer()); Assert.Single(d.Outbox); Assert.Single(d.Avisos); Assert.NotNull(d.Outbox.Single().Procesado);
        Assert.Equal(1, s.Metricas.Pagos); Assert.Equal(EstadoPedido.VALIDADO, d.Pedidos.Single(x => x.Id == b).Estado);
        await s.EvidenciaAsync("R06-RECUPERADO-" + inicial, "B sin cobro, A PAGADO165 con referencia original, un movimiento/evento/aviso tras reinicios y Outbox reintentado", pago);
    }

    [Fact]
    public async Task UnidadesLibresDelMismoSkuYOtroSku_PermitenVentasYNoQuedaReservaTrasCommit()
    {
        await using var e = await Escenario.CrearAsync();
        var a = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 2));
        var excesivo = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 3));
        var libre = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 2));
        e.Demo.FallarPersistencia = true; await Assert.ThrowsAsync<IOException>(() => e.Facade.ConfirmarAsync(a, "reserva-dos")); e.Demo.FallarPersistencia = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Facade.ConfirmarAsync(excesivo, "no-tres"));
        await e.Facade.ConfirmarAsync(libre, "libres-dos");
        await e.PagadoAsync("Luis", TipoEntrega.ESTANDAR, ("P05", 1));
        await using var r = new Escenario(e.Config.Ruta); await r.InicializarAsync(); await r.Facade.ConfirmarAsync(a, "consumir-propias");
        var d = await r.Consultas.CargarAsync(); Assert.Equal(3, d.Pagos.Count); Assert.Equal(3, r.Diario.Leer().Count);
        Assert.Equal(0, d.Productos.Single(x => x.Sku == "P04").Stock); Assert.Equal(29, d.Productos.Single(x => x.Sku == "P05").Stock);
        await r.Catalogo.AjustarStockAsync("P04", 2, "Unidades nuevas libres tras consumir reservas");
        await r.PagadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 2));
        d = await r.Consultas.CargarAsync(); Assert.Equal(4, d.Pagos.Count); Assert.Equal(0, d.Productos.Single(x => x.Sku == "P04").Stock);
        await r.EvidenciaAsync("R06-UNIDADES-LIBRES", "Pendiente reserva solo2; otra venta consume2 libres y otroSKU; recuperar y reponer no deja reserva fantasma", d.Pagos);
    }

    [Fact]
    public async Task KitsYLineasRepetidas_AgrupanSkuDeInstantaneasPersistidas()
    {
        await using var e = await Escenario.CrearAsync();
        var a = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("KIT_TELETRABAJO", 1), ("KIT_OFICINA", 1), ("P01", 1));
        var b = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P01", 6));
        e.Demo.FallarPersistencia = true; await Assert.ThrowsAsync<IOException>(() => e.Facade.ConfirmarAsync(a, "kit-pendiente")); e.Demo.FallarPersistencia = false;
        await e.Catalogo.GuardarKitAsync("KIT_TELETRABAJO", "Composicion posterior", [new("P05", 1)]);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => e.Facade.ConfirmarAsync(b, "kit-ocupado")); Assert.Contains("disponible 5", error.Message);
        await e.PagadoAsync("Luis", TipoEntrega.ESTANDAR, ("P01", 5), ("P02", 3), ("P03", 19));
        await using var r = new Escenario(e.Config.Ruta); await r.InicializarAsync(); var pago = await r.Facade.ConfirmarAsync(a, "kit-recuperado");
        Assert.Equal(135, pago.Importe); var d = await r.Consultas.CargarAsync(); var movimientos = d.Movimientos.Where(x => x.PedidoId == a).OrderBy(x => x.Sku).ToList();
        Assert.Equal(new[] { "P01", "P02", "P03" }, movimientos.Select(x => x.Sku)); Assert.Equal(new[] { -3, -2, -1 }, movimientos.Select(x => x.Cantidad));
        Assert.Equal(2, d.Pagos.Count); Assert.Equal(2, r.Diario.Leer().Count); Assert.All(d.Productos.Where(x => x.Sku is "P01" or "P02" or "P03"), x => Assert.Equal(0, x.Stock));
        await r.EvidenciaAsync("R06-KITS-AGREGADOS", "Compromiso P01x3/P02x2/P03x1 de snapshots; resto libre vendible aunque cambie el kit actual", new { pago, movimientos });
    }

    [Theory]
    [InlineData("LEGADO")]
    [InlineData("ALTERNATIVO")]
    public async Task RechazoYCancelacionPermitida_NoComprometenExistencias(string proveedor)
    {
        await using var e = await Escenario.CrearAsync();
        var a = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 4));
        var b = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 4));
        e.Demo.Proveedor = proveedor; e.Demo.FallarCobro = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Facade.ConfirmarAsync(a, "rechazado"));
        Assert.Empty(e.Diario.Leer()); await e.Pedidos.CancelarAsync(a); e.Demo.FallarCobro = false;
        var pago = await e.Facade.ConfirmarAsync(b, "aceptado"); Assert.Equal(165, pago.Importe);
        var d = await e.Consultas.CargarAsync(); Assert.Single(d.Pagos); Assert.Single(d.Movimientos); Assert.Single(e.Diario.Leer());
        Assert.Equal(EstadoPedido.CANCELADO, d.Pedidos.Single(x => x.Id == a).Estado); Assert.Equal(0, d.Productos.Single(x => x.Sku == "P04").Stock);
        await e.EvidenciaAsync("R06-RECHAZO-" + proveedor, "Rechazo no retiene unidades; cancelar impago y cobrar otro pedido usa las4 libres", pago);
    }

    [Fact]
    public async Task RechazoLuegoAceptacionConRollback_SoloComprometeTrasAceptar()
    {
        await using var e = await Escenario.CrearAsync();
        var a = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 2));
        var b = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 2));
        var c = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 2));
        e.Demo.FallarCobro = true; await Assert.ThrowsAsync<InvalidOperationException>(() => e.Facade.ConfirmarAsync(a, "rechazo-inicial")); e.Demo.FallarCobro = false;
        await e.Facade.ConfirmarAsync(b, "venta-libre"); e.Demo.FallarPersistencia = true;
        await Assert.ThrowsAsync<IOException>(() => e.Facade.ConfirmarAsync(a, "aceptacion-pendiente")); e.Demo.FallarPersistencia = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Facade.ConfirmarAsync(c, "sin-unidades"));
        await e.Facade.ConfirmarAsync(a, "conciliar"); Assert.Equal(2, e.Diario.Leer().Count);
        var d = await e.Consultas.CargarAsync(); Assert.Equal(2, d.Pagos.Count); Assert.Equal(0, d.Productos.Single(x => x.Sku == "P04").Stock);
    }

    [Fact]
    public async Task AjustesNegativos_RespetanComprometidoYPermitenReducirSoloLibre()
    {
        await using var e = await Escenario.CrearAsync(); var a = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 2));
        e.Demo.FallarPersistencia = true; await Assert.ThrowsAsync<IOException>(() => e.Facade.ConfirmarAsync(a, "ajuste-pendiente")); e.Demo.FallarPersistencia = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Catalogo.AjustarStockAsync("P04", -3, "No tomar unidades comprometidas"));
        Assert.Empty((await e.Consultas.CargarAsync()).Movimientos);
        await e.Catalogo.AjustarStockAsync("P04", -2, "Retirar solo las unidades libres");
        await using var r = new Escenario(e.Config.Ruta); await r.InicializarAsync(); await r.Facade.ConfirmarAsync(a, "ajuste-recuperacion");
        var d = await r.Consultas.CargarAsync(); Assert.Equal(2, d.Movimientos.Count); Assert.Equal(0, d.Productos.Single(x => x.Sku == "P04").Stock); Assert.Single(d.Pagos);
        await r.EvidenciaAsync("R06-AJUSTES", "Ajuste-3 rechazado; ajuste-2 libre permitido; compromiso2 sigue recuperable", d.Movimientos);
    }

    [Fact]
    public async Task ValidarOtroBorrador_NoPrometeUnidadesComprometidas()
    {
        await using var e = await Escenario.CrearAsync(); var a = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 4));
        var b = await e.BorradorAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 4));
        e.Demo.FallarPersistencia = true; await Assert.ThrowsAsync<IOException>(() => e.Facade.ConfirmarAsync(a, "validacion-pendiente")); e.Demo.FallarPersistencia = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Pedidos.ValidarAsync(b));
        Assert.Equal(EstadoPedido.BORRADOR, (await e.Consultas.CargarAsync()).Pedidos.Single(x => x.Id == b).Estado);
    }

    [Fact]
    public async Task DosInstancias_RecuperacionYVentaCompetidora_ConservanUnSoloPago()
    {
        await using var e = await Escenario.CrearAsync(); var a = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 4));
        var b = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 4));
        e.Demo.FallarPersistencia = true; await Assert.ThrowsAsync<IOException>(() => e.Facade.ConfirmarAsync(a, "concurrente-pendiente"));
        await using var uno = new Escenario(e.Config.Ruta); await uno.ComoAsync(LogiMarket.Application.Contratos.Rol.VENTAS);
        await using var dos = new Escenario(e.Config.Ruta); await dos.ComoAsync(LogiMarket.Application.Contratos.Rol.VENTAS); dos.Demo.Proveedor = "ALTERNATIVO";
        using var inicio = new ManualResetEventSlim(false);
        var recuperar = Task.Run(async () => { inicio.Wait(); return await uno.Facade.ConfirmarAsync(a, "concurrente-A"); });
        var competir = Task.Run(async () => { inicio.Wait(); return await Record.ExceptionAsync(() => dos.Facade.ConfirmarAsync(b, "concurrente-B")); });
        inicio.Set(); var pago = await recuperar; var error = await competir;
        Assert.Contains("STOCK", Assert.IsType<InvalidOperationException>(error).Message); Assert.Equal(a, pago.PedidoId);
        var d = await uno.Consultas.CargarAsync(); Assert.Single(d.Pagos); Assert.Single(d.Movimientos); Assert.Single(uno.Diario.Leer()); Assert.Equal(0, d.Productos.Single(x => x.Sku == "P04").Stock);
        await uno.Outbox.ProcesarAsync(); await dos.Outbox.ProcesarAsync(); Assert.Single((await dos.Consultas.CargarAsync()).Avisos);
        await uno.EvidenciaAsync("R06-DOS-INSTANCIAS", "Recuperar A y competir B concurrentemente: A consume su compromiso y B no cobra", pago);
    }

    [Fact]
    public async Task DosAceptacionesNuevasConcurrentesConFalloLocal_NoComprometenElMismoStock()
    {
        await using var e = await Escenario.CrearAsync(); var a = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 4));
        var b = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 4));
        await using var uno = new Escenario(e.Config.Ruta); await uno.ComoAsync(LogiMarket.Application.Contratos.Rol.VENTAS); uno.Demo.FallarPersistencia = true;
        await using var dos = new Escenario(e.Config.Ruta); await dos.ComoAsync(LogiMarket.Application.Contratos.Rol.VENTAS); dos.Demo.FallarPersistencia = true;
        using var inicio = new ManualResetEventSlim(false);
        var tareaA = Task.Run(async () => { inicio.Wait(); return await Record.ExceptionAsync(() => uno.Facade.ConfirmarAsync(a, "nueva-A")); });
        var tareaB = Task.Run(async () => { inicio.Wait(); return await Record.ExceptionAsync(() => dos.Facade.ConfirmarAsync(b, "nueva-B")); });
        inicio.Set(); var errores = await Task.WhenAll(tareaA, tareaB); Assert.Single(errores.OfType<IOException>()); Assert.Single(errores.OfType<InvalidOperationException>());
        var aceptado = Assert.Single(uno.Diario.Leer()); Assert.NotNull(aceptado.PedidoId);
        await using var r = new Escenario(e.Config.Ruta); await r.InicializarAsync(); var pago = await r.Facade.ConfirmarAsync(aceptado.PedidoId!.Value, "recuperacion-ganador");
        Assert.Equal(aceptado.Referencia, pago.Referencia); var d = await r.Consultas.CargarAsync(); Assert.Single(d.Pagos); Assert.Single(d.Movimientos); Assert.Equal(0, d.Productos.Single(x => x.Sku == "P04").Stock);
        await r.EvidenciaAsync("R06-ACEPTACIONES-CONCURRENTES", "Solo una aceptacion165 pese a rollback; segunda instancia observa compromiso durable y rechaza antes del proveedor", new { pago, Errores = errores.Select(x => x?.Message) });
    }
}
