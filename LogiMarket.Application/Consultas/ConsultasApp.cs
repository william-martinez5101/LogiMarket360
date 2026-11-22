using LogiMarket.Application.Contratos;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Application.Consultas;

public sealed record EstadoConsultas(IReadOnlyList<ProductoSimple> Productos, IReadOnlyList<KitCompuesto> Kits,
    IReadOnlyList<Cliente> Clientes, IReadOnlyList<Pedido> Pedidos, IReadOnlyList<PagoRegistrado> Pagos,
    IReadOnlyList<MovimientoInventario> Movimientos, IReadOnlyList<DespachoRegistrado> Despachos,
    IReadOnlyList<DevolucionRegistrada> Devoluciones, IReadOnlyList<EventoPendiente> Outbox,
    IReadOnlyList<EntradaBitacora> Auditoria, IReadOnlyList<AvisoRegistrado> Avisos);
public sealed class ConsultasApp(IUnidadTrabajoFactory unidades, SesionUsuario sesion, MetricasSesion metricas)
{
    public async Task<EstadoConsultas> CargarAsync()
    {
        sesion.Exigir(); await using var u = await unidades.AbrirAsync(false);
        metricas.Sincronizar(await u.Eventos.MetricasAsync());
        return new(await u.Catalogo.ProductosAsync(), await u.Catalogo.KitsAsync(), await u.Clientes.ListarAsync(),
            await u.Pedidos.ListarAsync(), await u.Operaciones.PagosAsync(), await u.Operaciones.MovimientosAsync(),
            await u.Operaciones.DespachosAsync(), await u.Operaciones.DevolucionesAsync(), await u.Eventos.ListarAsync(),
            await u.Bitacora.ListarAsync(), await u.Eventos.AvisosAsync());
    }
    public async Task<IReadOnlyList<Pedido>> FiltrarPedidosAsync(string expresion, DateTimeOffset? desde = null, DateTimeOffset? hasta = null)
    {
        sesion.Exigir(); var ast = string.IsNullOrWhiteSpace(expresion) ? null : new FiltroPedidosParser().Interpretar(expresion);
        await using var u = await unidades.AbrirAsync(false);
        return (await u.Pedidos.ListarAsync()).Where(p => (ast?.Evaluar(p) ?? true) && (desde is null || p.Creado >= desde) && (hasta is null || p.Creado <= hasta)).ToList();
    }
}
