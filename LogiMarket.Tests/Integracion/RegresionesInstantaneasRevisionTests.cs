using LogiMarket.Domain.Ventas;

namespace LogiMarket.Tests.Integracion;

public sealed class RegresionesInstantaneasRevisionTests
{
    [Theory]
    [InlineData("P01", 1, 1)]
    [InlineData("P01", 999, 8)]
    [InlineData("KIT_TELETRABAJO", 1, 7)]
    [InlineData("KIT_TELETRABAJO", 900, 1)]
    public async Task NuevasLineas_SeCapturanDelCatalogoNoDelLlamador(string codigo, decimal precio, int cantidadRaiz)
    {
        await using var e = await Escenario.CrearAsync(); var d = await e.Consultas.CargarAsync();
        var b = new PedidoBorrador(); b.SeleccionarCliente(d.Clientes.Single(x => x.Nombre == "Luis").Id);
        b.Agregar(new(new(codigo, "Nombre inventado", cantidadRaiz, precio, 100,
            [new("P05", "Composicion inventada", 20, 1, 90, [])]), 1));
        var id = await e.Pedidos.RegistrarAsync(b); await e.Pedidos.ValidarAsync(id);
        var pago = await e.Facade.ConfirmarAsync(id, "canonico"); var p = (await e.Consultas.CargarAsync()).Pedidos.Single();
        Assert.Equal(codigo == "P01" ? 25 : 65, pago.Importe); Assert.Equal(1, p.Lineas.Single().Elemento.Cantidad);
        Assert.Equal(codigo == "P01" ? new[] { "P01" } : ["P01", "P02", "P03"], p.Fisicos().Select(x => x.Sku).Order().ToArray());
        Assert.All(p.Fisicos(), x => Assert.Equal(1, x.Cantidad));
        await e.EvidenciaAsync($"R04-NUEVA-{codigo}-{precio}", "Precio, raiz y composicion capturados del catalogo en Application", new { pago, p.Lineas });
    }

    [Fact]
    public async Task EditarBorrador_ConservaHistoricasYCapturaNuevasIncluidosDuplicados()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.BorradorAsync("Luis", TipoEntrega.ESTANDAR, ("P01", 1));
        var anterior = (await e.Consultas.CargarAsync()).Pedidos.Single();
        await e.Catalogo.GuardarProductoAsync("P01", "Mouse nuevo", 30, .4m, 999);
        var b = new PedidoBorrador(); b.SeleccionarCliente(anterior.ClienteId);
        b.Agregar(anterior.Lineas[0].Copiar(2)); b.Agregar(anterior.Lineas[0]);
        await e.Pedidos.RegistrarAsync(b, id, anterior.Version); var p = (await e.Consultas.CargarAsync()).Pedidos.Single();
        Assert.Equal(new[] { 20m, 30m }, p.Lineas.Select(x => x.PrecioUnitario)); Assert.Equal(75, p.Valores.Total);
        Assert.Equal("Mouse USB", p.Lineas[0].Elemento.Nombre); Assert.Equal("Mouse nuevo", p.Lineas[1].Elemento.Nombre);
        await using var r = new Escenario(e.Config.Ruta); await r.InicializarAsync();
        var persistido = (await r.Consultas.CargarAsync()).Pedidos.Single(); Assert.Equal(p.Valores, persistido.Valores);
        Assert.Equal(new[] { 20m, 30m }, persistido.Lineas.Select(x => x.PrecioUnitario));
    }

    [Fact]
    public async Task CopiaAlteradaDeLineaHistorica_NoIntroducePrecioNiComposicionAjena()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.BorradorAsync(); var anterior = (await e.Consultas.CargarAsync()).Pedidos.Single();
        var b = new PedidoBorrador(); b.SeleccionarCliente(anterior.ClienteId);
        var raiz = anterior.Lineas[0].Elemento;
        b.Agregar(new(new(raiz.Codigo, raiz.Nombre, 1, raiz.Precio, 0, [new("P05", "Alfombrilla", 2, 5, .1m, [])]), 1));
        await e.Pedidos.RegistrarAsync(b, id, anterior.Version); var p = (await e.Consultas.CargarAsync()).Pedidos.Single();
        Assert.Equal(new[] { "P01", "P02", "P03" }, p.Fisicos().Select(x => x.Sku).Order()); Assert.Equal(59, p.Valores.Total);
    }

    [Fact]
    public async Task CambioCatalogo_NoRecalculaPagoNiDevolucionHistoricos()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.EntregadoAsync(); var antes = (await e.Consultas.CargarAsync()).Pedidos.Single();
        await e.Catalogo.GuardarProductoAsync("P01", "Mouse nuevo", 200, 2, 8);
        await e.Catalogo.GuardarKitAsync("KIT_TELETRABAJO", "Composicion nueva", [new("P05", 2)]);
        var devolucion = await e.Devoluciones.ProcesarAsync(new(id, "DEFECTO", true));
        var d = await e.Consultas.CargarAsync(); Assert.Equal(59, d.Pagos.Single().Importe); Assert.Equal(54, devolucion.Reembolso);
        Assert.Equal(antes.Valores, d.Pedidos.Single().Valores); Assert.Equal(new[] { "P01", "P02", "P03" }, d.Pedidos.Single().Fisicos().Select(x => x.Sku).Order());
        await e.EvidenciaAsync("R04-HISTORICO", "Cambiar precios/composicion no modifica el pago 59 ni el reembolso 54 del pedido entregado", new { devolucion, d.Pagos });
    }

    [Fact]
    public async Task CodigoNoCatalogado_NoSePersisteNiCobra()
    {
        await using var e = await Escenario.CrearAsync(); var d = await e.Consultas.CargarAsync(); var b = new PedidoBorrador(); b.SeleccionarCliente(d.Clientes.First().Id);
        b.Agregar(new(new("INVENTADO", "Ajeno", 1, 1, 0, []), 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Pedidos.RegistrarAsync(b));
        var despues = await e.Consultas.CargarAsync(); Assert.Empty(despues.Pedidos); Assert.Empty(despues.Pagos); Assert.Empty(despues.Outbox); Assert.Empty(e.Diario.Leer());
    }
}
