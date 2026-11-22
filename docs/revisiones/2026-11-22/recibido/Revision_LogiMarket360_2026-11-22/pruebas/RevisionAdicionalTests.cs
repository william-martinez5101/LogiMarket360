using LogiMarket.Application.Contratos;
using LogiMarket.Application.Consultas;
using LogiMarket.Domain.Ventas;
using LogiMarket.Tests.Integracion;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace RevisionLogiMarket360;

// Estas pruebas adicionales se ejecutan sobre una copia para revision.
// El codigo original del programa y sus 109 pruebas se conservan intactos.
public sealed class RevisionAdicionalTests(ITestOutputHelper salida)
{
    [Fact]
    public async Task REV01_ReintentoConOtraClaveTrasRollback_NoDebeDuplicarCobro()
    {
        await using var e = await Escenario.CrearAsync();
        var pedidoId = await e.ValidadoAsync();
        e.Demo.FallarPersistencia = true;

        await Assert.ThrowsAsync<IOException>(() =>
            e.Facade.ConfirmarAsync(pedidoId, "revision-intento-1"));

        e.Demo.FallarPersistencia = false;
        await e.Facade.ConfirmarAsync(pedidoId, "revision-intento-2");
        var datos = await e.Consultas.CargarAsync();
        var cobros = e.Diario.Leer();

        salida.WriteLine($"Pedido: {pedidoId}");
        salida.WriteLine($"Pagos locales: {datos.Pagos.Count}");
        salida.WriteLine($"Cobros aceptados del proveedor: {cobros.Count}");
        salida.WriteLine($"Importe aceptado acumulado: {cobros.Sum(c => c.Monto)}");
        foreach (var cobro in cobros)
            salida.WriteLine($"{cobro.Clave}: {cobro.Monto}, {cobro.Referencia}");

        Assert.Single(datos.Pagos);
        Assert.Single(cobros);
    }

    [Fact]
    public async Task REV02_ProductoEliminado_DebeSeguirEliminadoTrasReiniciar()
    {
        await using var e = await Escenario.CrearAsync();
        await e.Catalogo.EliminarProductoAsync("P04");
        var antes = await e.Consultas.CargarAsync();
        Assert.DoesNotContain(antes.Productos, p => p.Sku == "P04");

        await using var reinicio = new Escenario(e.Config.Ruta);
        await reinicio.InicializarAsync();
        var despues = await reinicio.Consultas.CargarAsync();

        salida.WriteLine($"Productos antes del reinicio: {antes.Productos.Count}");
        salida.WriteLine($"Productos despues del reinicio: {despues.Productos.Count}");
        salida.WriteLine($"P04 reaparece: {despues.Productos.Any(p => p.Sku == "P04")}");
        Assert.DoesNotContain(despues.Productos, p => p.Sku == "P04");
    }

    [Fact]
    public async Task REV03_RegistrarPedido_NoDebeAceptarPrecioInventadoPorElLlamador()
    {
        await using var e = await Escenario.CrearAsync();
        await e.ComoAsync(Rol.VENTAS);
        var datos = await e.Consultas.CargarAsync();
        var precioCatalogo = datos.Productos.Single(p => p.Sku == "P01").Precio;
        var borrador = new PedidoBorrador();
        borrador.SeleccionarCliente(datos.Clientes.Single(c => c.Nombre == "Luis").Id);
        borrador.Agregar(new LineaPedido(
            new NodoInstantanea("P01", "Mouse USB", 1, 1m, .20m, []), 1));

        LogiMarket.Application.Contratos.PagoRegistrado pago;
        try
        {
            var id = await e.Pedidos.RegistrarAsync(borrador);
            await e.Pedidos.ValidarAsync(id);
            pago = await e.Facade.ConfirmarAsync(id, "revision-precio");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            // Rechazar la instantanea manipulada antes de cobrar tambien es correcto.
            salida.WriteLine($"Entrada rechazada: {ex.Message}");
            Assert.Empty((await e.Consultas.CargarAsync()).Pagos);
            Assert.Empty(e.Diario.Leer());
            return;
        }

        salida.WriteLine($"Precio real del SKU P01: {precioCatalogo}");
        salida.WriteLine("Precio aportado por el llamador: 1.00");
        salida.WriteLine($"Total cobrado con tarifa 5.00: {pago.Importe}");
        Assert.Equal(precioCatalogo + 5m, pago.Importe);
    }

    [Theory]
    [InlineData("CLIENTE = =")]
    [InlineData("CLIENTE = >=")]
    public void REV04_Interpreter_RechazaUnOperadorComoValorDeCliente(string filtro)
    {
        salida.WriteLine($"Filtro invalido: {filtro}");
        Assert.Throws<FormatException>(() => new FiltroPedidosParser().Interpretar(filtro));
    }
}
