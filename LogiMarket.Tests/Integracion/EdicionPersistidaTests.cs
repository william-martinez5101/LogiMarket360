using LogiMarket.Domain.Ventas;
namespace LogiMarket.Tests.Integracion;

public sealed class EdicionPersistidaTests
{
    [Fact] public async Task ReducirYReordenarLineasConservaSoloLaInstantaneaFinal()
    {
        await using var e = await Escenario.CrearAsync();
        var id = await e.BorradorAsync("Luis", TipoEntrega.ESTANDAR, ("P01", 1), ("P02", 2), ("P03", 3));
        var inicial = (await e.Consultas.CargarAsync()).Pedidos.Single();
        var b = new PedidoBorrador(); b.SeleccionarCliente(inicial.ClienteId); b.SeleccionarEntrega(TipoEntrega.EXPRES);
        b.Agregar(inicial.Lineas[2].Copiar(2));
        await e.Pedidos.RegistrarAsync(b, id, inicial.Version);
        var final = (await e.Consultas.CargarAsync()).Pedidos.Single();
        Assert.Single(final.Lineas); Assert.Equal("P03", final.Lineas[0].Codigo); Assert.Equal(2, final.Lineas[0].Cantidad);
        Assert.Equal(32, final.Valores.Total); Assert.Equal(TipoEntrega.EXPRES, final.Entrega.Tipo);
        var claveVieja = inicial.Version;
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Pedidos.RegistrarAsync(b, id, claveVieja));
        Assert.Equal(final.Version, (await e.Consultas.CargarAsync()).Pedidos.Single().Version);
    }
}
