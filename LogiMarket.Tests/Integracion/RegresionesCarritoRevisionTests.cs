using LogiMarket.Application.Presentacion;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Tests.Integracion;

public sealed class RegresionesCarritoRevisionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProyeccionOrdenada_DistingueDuplicadosRestauraHistorialYGuarda(bool descendente)
    {
        await using var e = await Escenario.CrearAsync(); var d = await e.Consultas.CargarAsync(); var b = new PedidoBorrador();
        b.SeleccionarCliente(d.Clientes.Single(x => x.Nombre == "Luis").Id);
        foreach (var (sku, cantidad) in new[] { ("P04", 1), ("P01", 1), ("P01", 3) })
            b.Agregar(new(NodoInstantanea.Capturar(d.Productos.Single(x => x.Sku == sku)), cantidad));
        var h = new HistorialBorrador(b); var filas = FilaLineaCarrito.Crear(b.Lineas);
        var ordenadas = (descendente ? filas.OrderByDescending(x => x.Articulo) : filas.OrderBy(x => x.Articulo)).ToList();
        var duplicado = ordenadas.Single(x => x.Articulo == "P01" && x.Cantidad == 3);
        h.Editar(x => x.CambiarCantidad(duplicado.IndiceEn(x.Lineas), 2));
        Assert.Equal(new[] { 1, 1, 2 }, b.Lineas.Select(x => x.Cantidad));
        h.Deshacer(); Assert.Equal(new[] { 1, 1, 3 }, b.Lineas.Select(x => x.Cantidad)); h.Rehacer();
        var quitar = FilaLineaCarrito.Crear(b.Lineas).OrderByDescending(x => x.Articulo).Single(x => x.Articulo == "P01" && x.Cantidad == 1);
        h.Editar(x => x.Quitar(quitar.IndiceEn(x.Lineas))); Assert.Equal(new[] { "P04", "P01" }, b.Lineas.Select(x => x.Codigo));
        h.Deshacer(); Assert.Equal(3, b.Lineas.Count); h.Rehacer(); Assert.Equal(new[] { 1, 2 }, b.Lineas.Select(x => x.Cantidad));
        var id = await e.Pedidos.RegistrarAsync(b); await using var r = new Escenario(e.Config.Ruta); await r.InicializarAsync();
        var p = (await r.Consultas.CargarAsync()).Pedidos.Single(x => x.Id == id);
        Assert.Equal(new[] { "P04", "P01" }, p.Lineas.Select(x => x.Codigo)); Assert.Equal(new[] { 1, 2 }, p.Lineas.Select(x => x.Cantidad)); Assert.Equal(85, p.Valores.Total);
        await r.EvidenciaAsync("R03-" + (descendente ? "DESC" : "ASC"), "Seleccion enlazada, SKU repetido, cambio/quitar, deshacer/rehacer y recarga conservan articulo y cantidad", new { p.Lineas, p.Valores });
    }
}
