using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Tests.Domain;

public class VentasTests
{
    public static CalculadoraPedido Calculadora() => new([new Mayorista15(), new Vip10(), new SinDescuento()]);
    public static PedidoBuilder Builder() => new(Calculadora(), [new EntregaEstandarFactory(), new EntregaExpresFactory()]);
    public static KitCompuesto Teletrabajo()
    {
        var oficina = new KitCompuesto("KIT_OFICINA", "Oficina");
        oficina.Agregar(new ProductoSimple("P01", "Mouse USB", 20, .2m, 8));
        oficina.Agregar(new ProductoSimple("P02", "Teclado", 30, .7m, 5));
        var tele = new KitCompuesto("KIT_TELETRABAJO", "Teletrabajo"); tele.Agregar(oficina);
        tele.Agregar(new ProductoSimple("P03", "Memoria USB", 10, .1m, 20)); return tele;
    }
    [Fact] public void IteratorYVisitor_RecorrenAnidado()
    {
        var kit = Teletrabajo(); using var iter = new IteradorFisico(kit, 2);
        var hojas = new List<HojaCatalogo>(); while (iter.MoveNext()) hojas.Add(iter.Current);
        Assert.Equal(new[] { "P01", "P02", "P03" }, hojas.Select(x => x.Producto.Sku));
        Assert.All(hojas, x => Assert.Equal(2, x.Cantidad)); Assert.False(iter.MoveNext());
        Assert.Throws<InvalidOperationException>(() => iter.Current);
        var peso = new PesoLogisticoVisitor(); var detalle = new DesgloseFisicoVisitor(); kit.Aceptar(peso); kit.Aceptar(detalle);
        Assert.Equal(1m, peso.PesoKg); Assert.Equal(3, detalle.Unidades.Count);
    }
    [Theory] [InlineData(CategoriaCliente.Vip, 1, 6, 59)] [InlineData(CategoriaCliente.Normal, 1, 0, 65)]
    [InlineData(CategoriaCliente.Vip, 4, 36, 209)] [InlineData(CategoriaCliente.Mayorista, 1, 0, 65)]
    public void Promociones_Excluyentes(CategoriaCliente categoria, int cantidad, decimal descuento, decimal total)
    {
        var p = Builder().ParaCliente(new Cliente("Prueba", categoria)).Agregar(Teletrabajo(), cantidad).Construir(DateTimeOffset.UtcNow, "test");
        Assert.Equal(descuento, p.Valores.Descuento); Assert.Equal(total, p.Valores.Total);
    }
    [Fact] public void BuilderPrototypeYSnapshots_AislanCatalogo()
    {
        Assert.Throws<InvalidOperationException>(() => Builder().Construir(DateTimeOffset.UtcNow, "test"));
        var kit = Teletrabajo(); var p = Builder().ParaCliente(new Cliente("Ana", CategoriaCliente.Vip)).Agregar(kit, 1).Construir(DateTimeOffset.UtcNow, "test");
        var copia = p.ClonarBorrador(DateTimeOffset.UtcNow, "test");
        ((ProductoSimple)((KitCompuesto)kit.Componentes[0].Elemento).Componentes[0].Elemento).ActualizarDatos("Mouse nuevo", 100, 1);
        Assert.NotEqual(p.Id, copia.Id); Assert.NotSame(p.Lineas[0].Elemento.Hijos, copia.Lineas[0].Elemento.Hijos);
        Assert.Equal(59, copia.Valores.Total); Assert.Equal(59, p.Valores.Total);
    }
    [Fact] public void State_NoPermiteEditarNiCancelarPagado()
    {
        var p = Builder().ParaCliente(new Cliente("Ana", CategoriaCliente.Vip)).Agregar(Teletrabajo(), 1).Construir(DateTimeOffset.UtcNow, "test");
        var ahora = DateTimeOffset.UtcNow; p.Validar(ahora, "test"); p.Pagar(ahora, "test");
        Assert.Throws<InvalidOperationException>(() => p.Cancelar(ahora, "test"));
        Assert.Throws<InvalidOperationException>(() => p.EditarDesde(p));
        Assert.Equal(3, p.Historial.Count); p.Despachar(ahora, "test"); p.Entregar(ahora, "test");
        Assert.Equal(EstadoPedido.ENTREGADO, p.Estado);
    }
    [Fact] public void Memento_DeshaceCompletoYCortaRama()
    {
        var b = new PedidoBorrador(); var h = new HistorialBorrador(b); var cliente = Guid.NewGuid();
        h.Editar(x => x.SeleccionarCliente(cliente)); h.Editar(x => x.Agregar(new(NodoInstantanea.Capturar(Teletrabajo()), 1)));
        h.Editar(x => x.CambiarCantidad(0, 3)); h.Deshacer(); h.Deshacer(); Assert.Empty(b.Lineas); Assert.Equal(cliente, b.ClienteId);
        h.Rehacer(); Assert.Single(b.Lineas); Assert.Equal(1, b.Lineas[0].Cantidad);
        for (int i = 1; i < 9; i++) h.Editar(x => x.CambiarCantidad(0, i)); Assert.Equal(5, h.CantidadEstados);
        h.Confirmado(); Assert.False(h.PuedeDeshacer); Assert.Throws<InvalidOperationException>(() => b.CambiarCantidad(0, 2));
    }
    [Fact] public void Cantidades_NoDesbordan() => Assert.Throws<OverflowException>(() => Cantidades.Multiplicar(int.MaxValue, 2));
}
