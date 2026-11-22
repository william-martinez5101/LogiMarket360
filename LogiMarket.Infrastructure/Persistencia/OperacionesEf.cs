using LogiMarket.Application.Contratos;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia;

internal sealed class OperacionesEf(LogiMarketDbContext db) : IOperacionRepository, IEventoRepository, IBitacoraRepository
{
    public async Task<PagoRegistrado?> PagoPorClaveAsync(string clave)
    { var p = await db.Set<PagoFila>().SingleOrDefaultAsync(x => x.Clave == clave); return p is null ? null : Pago(p); }
    private static PagoRegistrado Pago(PagoFila p) => new(p.Id, p.PedidoId, p.Clave, p.Referencia, p.Importe, p.Proveedor, p.Fecha);
    public async Task<IReadOnlyList<PagoRegistrado>> PagosAsync() => (await db.Set<PagoFila>().ToListAsync()).Select(Pago).ToList();
    public Task AgregarPagoAsync(PagoRegistrado p) { db.Add(new PagoFila { Id = p.Id, PedidoId = p.PedidoId, Clave = p.Clave, Referencia = p.Referencia, Importe = p.Importe, Proveedor = p.Proveedor, Fecha = p.Fecha }); return Task.CompletedTask; }
    public Task AgregarMovimientoAsync(MovimientoInventario m) { db.Add(new MovimientoFila { Id = m.Id, PedidoId = m.PedidoId, Sku = m.Sku, Cantidad = m.Cantidad, Motivo = m.Motivo, Referencia = m.Referencia, Fecha = m.Fecha }); return Task.CompletedTask; }
    public async Task<IReadOnlyList<MovimientoInventario>> MovimientosAsync() => (await db.Set<MovimientoFila>().ToListAsync()).Select(x => new MovimientoInventario(x.Id, x.PedidoId, x.Sku, x.Cantidad, x.Motivo, x.Referencia, x.Fecha)).OrderByDescending(x => x.Fecha).ToList();
    public Task AgregarDespachoAsync(DespachoRegistrado d) { db.Add(new DespachoFila { PedidoId = d.PedidoId, Guia = d.Guia, Fecha = d.Fecha, Estimada = d.Estimada }); return Task.CompletedTask; }
    public async Task<IReadOnlyList<DespachoRegistrado>> DespachosAsync() => (await db.Set<DespachoFila>().ToListAsync()).Select(x => new DespachoRegistrado(x.PedidoId, x.Guia, x.Fecha, x.Estimada)).ToList();
    public Task AgregarDevolucionAsync(DevolucionRegistrada d) { db.Add(new DevolucionFila { Id = d.Id, PedidoId = d.PedidoId, Motivo = d.Motivo, Reembolso = d.Reembolso, Referencia = d.Referencia, Fecha = d.Fecha }); return Task.CompletedTask; }
    public async Task<IReadOnlyList<DevolucionRegistrada>> DevolucionesAsync() => (await db.Set<DevolucionFila>().ToListAsync()).Select(x => new DevolucionRegistrada(x.Id, x.PedidoId, x.Motivo, x.Reembolso, x.Referencia, x.Fecha)).ToList();
    public Task AgregarAsync(EventoNegocio e) { db.Add(new EventoFila { Id = e.Id, PedidoId = e.PedidoId, Tipo = e.Tipo, Importe = e.Importe, Canal = e.Canal, Fecha = e.Fecha }); return Task.CompletedTask; }
    async Task<IReadOnlyList<EventoPendiente>> IEventoRepository.ListarAsync() => (await db.Set<EventoFila>().ToListAsync())
        .Select(e => new EventoPendiente(new(e.Id, e.PedidoId, e.Tipo, e.Importe, e.Canal, e.Fecha), e.Intentos, e.Procesado, e.Error)).OrderBy(x => x.Evento.Fecha).ToList();
    public Task<bool> EntregadoAsync(Guid id, string suscriptor) => db.Set<EntregaEventoFila>().AnyAsync(x => x.EventoId == id && x.Suscriptor == suscriptor);
    public Task MarcarEntregaAsync(Guid id, string suscriptor) { db.Add(new EntregaEventoFila { EventoId = id, Suscriptor = suscriptor }); return Task.CompletedTask; }
    public async Task MarcarProcesadoAsync(Guid id, DateTimeOffset fecha)
    { var e = await db.Set<EventoFila>().SingleAsync(x => x.Id == id); if (e.Procesado is null) { e.Procesado = fecha; e.Intentos++; e.Error = null; } }
    public async Task RegistrarErrorAsync(Guid id, string error)
    { var e = await db.Set<EventoFila>().SingleAsync(x => x.Id == id); e.Intentos++; e.Error = error; }
    public Task AgregarAvisoAsync(AvisoRegistrado a) { db.Add(new AvisoFila { EventoId = a.EventoId, Canal = a.Canal, Mensaje = a.Mensaje, Fecha = a.Fecha }); return Task.CompletedTask; }
    public async Task<IReadOnlyList<AvisoRegistrado>> AvisosAsync() => (await db.Set<AvisoFila>().ToListAsync()).Select(x => new AvisoRegistrado(x.EventoId, x.Canal, x.Mensaje, x.Fecha)).ToList();
    public Task AgregarMetricaAsync(MetricaRegistrada m) { db.Add(new MetricaFila { EventoId = m.EventoId, Tipo = m.Tipo, Importe = m.Importe }); return Task.CompletedTask; }
    public async Task<IReadOnlyList<MetricaRegistrada>> MetricasAsync() => (await db.Set<MetricaFila>().ToListAsync()).Select(x => new MetricaRegistrada(x.EventoId, x.Tipo, x.Importe)).ToList();
    public Task AgregarAsync(EntradaBitacora e) { db.Add(new BitacoraFila { Id = e.Id, Tipo = e.Tipo, Correlacion = e.Correlacion, Mensaje = e.Mensaje, Fecha = e.Fecha }); return Task.CompletedTask; }
    async Task<IReadOnlyList<EntradaBitacora>> IBitacoraRepository.ListarAsync() => (await db.Set<BitacoraFila>().ToListAsync()).Select(x => new EntradaBitacora(x.Id, x.Tipo, x.Correlacion, x.Mensaje, x.Fecha)).OrderByDescending(x => x.Fecha).ToList();
}
