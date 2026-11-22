using LogiMarket.Domain.Ventas;
using LogiMarket.Tests.Integracion;
using Xunit;
using Xunit.Abstractions;

namespace LogiMarket.Tests.Integracion.RevisionR06;

public sealed class CobroPendienteStockTests(ITestOutputHelper salida)
{
    [Fact]
    public async Task AceptacionPendiente_NoDebePerderSusUnidadesPorOtraVenta()
    {
        await using var e = await Escenario.CrearAsync();
        var a = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 4));
        var b = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P04", 4));
        e.Demo.FallarPersistencia = true;
        await Assert.ThrowsAsync<IOException>(() => e.Facade.ConfirmarAsync(a, "pendiente-A"));
        e.Demo.FallarPersistencia = false;

        // Otra venta intenta consumir las mismas cuatro unidades tras el rollback.
        var errorB = await Record.ExceptionAsync(() => e.Facade.ConfirmarAsync(b, "venta-B"));
        await using var r = new Escenario(e.Config.Ruta);
        await r.InicializarAsync();
        var errorA = await Record.ExceptionAsync(() => r.Facade.ConfirmarAsync(a, "recuperar-A"));
        var d = await r.Consultas.CargarAsync();
        var errorCancelacion = await Record.ExceptionAsync(() => r.Pedidos.CancelarAsync(a));

        salida.WriteLine($"Pedido A: {a}; pedido B: {b}");
        salida.WriteLine($"Resultado venta B: {errorB?.Message ?? "APROBADA"}");
        salida.WriteLine($"Resultado recuperacion A: {errorA?.Message ?? "RECUPERADA"}");
        salida.WriteLine($"Estado A: {d.Pedidos.Single(p => p.Id == a).Estado}");
        salida.WriteLine($"Pagos locales de A: {d.Pagos.Count(p => p.PedidoId == a)}");
        salida.WriteLine($"Pagos locales de B: {d.Pagos.Count(p => p.PedidoId == b)}");
        salida.WriteLine($"Stock P04: {d.Productos.Single(p => p.Sku == "P04").Stock}");
        salida.WriteLine($"Cobros externos aceptados: {r.Diario.Leer().Count}");
        salida.WriteLine($"Importe externo acumulado: {r.Diario.Leer().Sum(c => c.Monto)}");
        salida.WriteLine($"Cancelar A: {errorCancelacion?.Message ?? "CANCELADA"}");
        foreach (var c in r.Diario.Leer())
            salida.WriteLine($"Cobro: pedido={c.PedidoId}; importe={c.Monto}; referencia={c.Referencia}");

        // La recuperacion prometida debe seguir siendo posible sin stock negativo.
        Assert.Null(errorA);
        Assert.Single(d.Pagos.Where(p => p.PedidoId == a));
        Assert.All(d.Productos, p => Assert.True(p.Stock >= 0));
    }
}

