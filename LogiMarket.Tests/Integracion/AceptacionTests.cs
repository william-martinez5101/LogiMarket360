using LogiMarket.Application.Contratos;
using LogiMarket.Application.Consultas;
using LogiMarket.Application.Documentos;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Presentacion;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Ventas;
using LogiMarket.Infrastructure.Documentos;
using LogiMarket.Infrastructure.Pagos;
using LogiMarket.Infrastructure.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.Tests.Integracion;

public sealed partial class AceptacionTests
{
    [Fact] public async Task P01_MigracionesYSeedIdempotente()
    {
        await using var e = new Escenario(); Assert.False(File.Exists(e.Config.Ruta)); await e.InicializarAsync(); await e.InicializarAsync();
        var d = await e.Consultas.CargarAsync(); Assert.Equal(5, d.Productos.Count); Assert.Equal(2, d.Kits.Count);
        await using var db = e.Config.CrearContexto(); Assert.Single(await db.Database.GetAppliedMigrationsAsync());
        foreach (var rol in Enum.GetValues<Rol>()) Assert.Equal(rol, (await e.ComoAsync(rol)).Rol);
        await e.ComoAsync(Rol.ADMIN); await e.EvidenciaAsync("P01", "5 productos, 2 kits, 4 roles, una migracion; seed sin duplicados", new { Productos = d.Productos.Count, Kits = d.Kits.Count, Migraciones = await db.Database.GetAppliedMigrationsAsync() });
    }
    [Fact] public async Task P02_Ana59()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.PagadoAsync(); var d = await e.Consultas.CargarAsync(); var p = d.Pedidos.Single();
        Assert.Equal(59, p.Valores.Total); Assert.Equal(EstadoPedido.PAGADO, p.Estado); Assert.Single(d.Pagos);
        Assert.Equal(new[] { 7, 4, 19 }, d.Productos.Take(3).Select(x => x.Stock));
        await e.EvidenciaAsync("P02", "Ana: 59.00; stock 7/4/19; un pago y PAGADO", new { id, p.Valores, p.Estado });
    }
    [Fact] public async Task P03_Luis102()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.PagadoAsync("Luis", TipoEntrega.EXPRES, ("P04", 2), ("P05", 2)); var d = await e.Consultas.CargarAsync();
        Assert.Equal(102, d.Pedidos.Single().Valores.Total); Assert.Single(d.Pagos); Assert.Equal(2, d.Productos.Single(x => x.Sku == "P04").Stock); Assert.Equal(28, d.Productos.Single(x => x.Sku == "P05").Stock);
        await e.EvidenciaAsync("P03", "Luis: 102.00; P04=2 P05=28; un pago", new { id, d.Pedidos.Single().Valores });
    }
    [Fact] public async Task P04_Carmen90()
    {
        await using var e = await Escenario.CrearAsync(); await e.PagadoAsync("Carmen", TipoEntrega.ESTANDAR, ("P03", 10)); var d = await e.Consultas.CargarAsync();
        Assert.Equal(90, d.Pedidos.Single().Valores.Total); Assert.Equal(15, d.Pedidos.Single().Valores.Descuento); Assert.Equal(10, d.Productos.Single(x => x.Sku == "P03").Stock);
        await e.EvidenciaAsync("P04", "90.00; descuento 15.00; P03=10", d.Pedidos.Single().Valores);
    }
    [Fact] public async Task P05_StockInsuficienteSinEfectos()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.BorradorAsync("Ana", TipoEntrega.ESTANDAR, ("KIT_TELETRABAJO", 6));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => e.Pedidos.ValidarAsync(id)); Assert.Contains("P02", ex.Message);
        var d = await e.Consultas.CargarAsync(); Assert.Empty(d.Pagos); Assert.Empty(d.Movimientos); Assert.Empty(d.Outbox); Assert.Empty(e.Diario.Leer());
        Assert.Equal(new[] { 8, 5, 20, 4, 30 }, d.Productos.Select(x => x.Stock));
        await e.EvidenciaAsync("P05", "Rechazo por P02; cero efectos", new { ex.Message });
    }
    [Fact] public async Task P06_CobroRechazadoSinEfectos()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.ValidadoAsync(); e.Demo.FallarCobro = true;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => e.Facade.ConfirmarAsync(id, "rechazo")); var d = await e.Consultas.CargarAsync();
        Assert.Equal(EstadoPedido.VALIDADO, d.Pedidos.Single().Estado); Assert.Empty(d.Pagos); Assert.Empty(d.Outbox); Assert.Empty(d.Movimientos); Assert.Empty(e.Diario.Leer());
        Assert.Equal(new[] { 8, 5, 20, 4, 30 }, d.Productos.Select(x => x.Stock)); await e.EvidenciaAsync("P06", "Rechazo sin pago, stock ni evento", new { ex.Message });
    }
    [Fact] public async Task P07_IdempotenciaTrasPagadoYReinicio()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.ValidadoAsync(); var primero = await e.Facade.ConfirmarAsync(id, "unica");
        await using var reinicio = new Escenario(e.Config.Ruta); await reinicio.InicializarAsync(); var repetido = await reinicio.Facade.ConfirmarAsync(id, "unica");
        Assert.Equal(primero, repetido); var d = await reinicio.Consultas.CargarAsync(); Assert.Single(d.Pagos); Assert.Equal(3, d.Movimientos.Count); Assert.Single(e.Diario.Leer());
        await reinicio.EvidenciaAsync("P07", "Mismo resultado tras reiniciar; un pago y un movimiento por SKU", new { primero, repetido });
    }
    [Fact] public async Task P08_PagadoInmutable()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.PagadoAsync(); var antes = (await e.Consultas.CargarAsync()).Pedidos.Single();
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Pedidos.CancelarAsync(id));
        var b = new PedidoBorrador(); b.SeleccionarCliente(antes.ClienteId); b.Agregar(new(new("P01", "Precio alterado", 1, 1, .2m, []), 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Pedidos.RegistrarAsync(b, id, antes.Version)); var despues = (await e.Consultas.CargarAsync()).Pedidos.Single();
        Assert.Equal(antes.Version, despues.Version); Assert.Equal(antes.Historial, despues.Historial); Assert.Equal(antes.Valores, despues.Valores);
        await e.EvidenciaAsync("P08", "Cancelar y editar PAGADO denegados; historial intacto", new { despues.Estado, despues.Version, despues.Valores });
    }
    [Fact] public async Task P09_DespachoYEntregaConBodega()
    {
        await using var e = await Escenario.CrearAsync(); var impago = await e.BorradorAsync(); var pagado = await e.PagadoAsync(); await e.ComoAsync(Rol.BODEGA);
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Pedidos.DespacharAsync(impago)); var guia = await e.Pedidos.DespacharAsync(pagado); await e.Pedidos.EntregarAsync(pagado);
        var d = await e.Consultas.CargarAsync(); Assert.Equal(EstadoPedido.ENTREGADO, d.Pedidos.Single(x => x.Id == pagado).Estado); Assert.Single(d.Despachos);
        Assert.Equal(EstadoPedido.BORRADOR, d.Pedidos.Single(x => x.Id == impago).Estado); await e.EvidenciaAsync("P09", "BODEGA despacha/entrega pagado; impago intacto", guia);
    }
    [Fact] public async Task P10_PersistenciaEntreInstancias()
    {
        await using var e = await Escenario.CrearAsync(); await e.PagadoAsync(); await e.Outbox.ProcesarAsync(); var antes = await e.Consultas.CargarAsync();
        await using var r = new Escenario(e.Config.Ruta); await r.InicializarAsync(); var d = await r.Consultas.CargarAsync();
        Assert.Equal(antes.Pedidos.Single().Valores, d.Pedidos.Single().Valores); Assert.Equal(antes.Pagos, d.Pagos); Assert.Equal(antes.Auditoria, d.Auditoria); Assert.Equal(antes.Productos.Select(x => x.Stock), d.Productos.Select(x => x.Stock));
        await r.EvidenciaAsync("P10", "Pedido, pago, stock y auditoria conservados al reabrir; seed conserva venta", new { Pedido = d.Pedidos.Single().Id, Auditorias = d.Auditoria.Count });
    }
}
