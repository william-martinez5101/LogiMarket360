using System.Collections.Concurrent;
using LogiMarket.Application.Contratos;

namespace LogiMarket.Application.Eventos;

public sealed class MetricasSesion
{
    private readonly ConcurrentDictionary<Guid, MetricaRegistrada> _eventos = new();
    public void Sincronizar(IEnumerable<MetricaRegistrada> metricas)
    { foreach (var m in metricas) _eventos.TryAdd(m.EventoId, m); }
    public int Pagos => _eventos.Values.Count(x => x.Tipo == "PedidoPagado");
    public int Despachos => _eventos.Values.Count(x => x.Tipo == "DespachoRegistrado");
    public int Devoluciones => _eventos.Values.Count(x => x.Tipo == "DevolucionAceptada");
    public decimal Ventas => _eventos.Values.Where(x => x.Tipo == "PedidoPagado").Sum(x => x.Importe);
    public decimal Reembolsos => _eventos.Values.Where(x => x.Tipo == "DevolucionAceptada").Sum(x => x.Importe);
}
public sealed class AuditoriaObserver(IReloj reloj) : IObservadorEvento
{
    public string Nombre => "AUDITORIA";
    public Task AplicarAsync(EventoNegocio e, IUnidadTrabajo u)
        => u.Bitacora.AgregarAsync(new(e.Id, "EVENTO", e.PedidoId.ToString(), $"{e.Tipo}; evento={e.Id}; importe={e.Importe:F2}", reloj.Ahora));
}
public sealed class MetricasObserver : IObservadorEvento
{
    public string Nombre => "METRICAS";
    public Task AplicarAsync(EventoNegocio e, IUnidadTrabajo u) => u.Eventos.AgregarMetricaAsync(new(e.Id, e.Tipo, e.Importe));
}
public sealed class NotificacionObserver(IEnumerable<CreadorNotificacion> creadores) : IObservadorEvento
{
    public string Nombre => "NOTIFICACION";
    public Task AplicarAsync(EventoNegocio e, IUnidadTrabajo u)
        => (creadores.SingleOrDefault(c => c.Canal == e.Canal) ?? throw new InvalidOperationException("Canal de notificacion desconocido.")).EnviarAsync(e, u);
}
public sealed class PublicadorEventos(IEnumerable<IObservadorEvento> observadores)
{
    public IReadOnlyList<IObservadorEvento> Suscriptores { get; } = observadores.ToList().AsReadOnly();
}
public sealed record ResultadoOutbox(int Procesados, int Pendientes);
public sealed class ProcesadorOutbox(IUnidadTrabajoFactory unidades, PublicadorEventos publicador,
    MetricasSesion metricas, IReloj reloj, IBitacoraTecnica tecnica)
{
    public async Task<ResultadoOutbox> ProcesarAsync(CancellationToken ct = default)
    {
        IReadOnlyList<EventoPendiente> eventos;
        await using (var lectura = await unidades.AbrirAsync(false, ct)) eventos = await lectura.Eventos.ListarAsync();
        foreach (var pendiente in eventos.Where(x => x.Procesado is null))
        {
            var e = pendiente.Evento; bool fallo = false;
            foreach (var observador in publicador.Suscriptores)
            {
                try
                {
                    await using var u = await unidades.AbrirAsync(true, ct);
                    if (await u.Eventos.EntregadoAsync(e.Id, observador.Nombre)) continue;
                    await observador.AplicarAsync(e, u);
                    await u.Eventos.MarcarEntregaAsync(e.Id, observador.Nombre); await u.ConfirmarAsync(ct);
                    tecnica.Registrar("OUTBOX_OK", e.Id.ToString(), observador.Nombre);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    fallo = true; tecnica.Registrar("OUTBOX_ERROR", e.Id.ToString(), $"{observador.Nombre}: {ex.Message}");
                    await using var error = await unidades.AbrirAsync(true, ct);
                    await error.Eventos.RegistrarErrorAsync(e.Id, ex.Message); await error.ConfirmarAsync(ct);
                }
            }
            if (!fallo)
            {
                await using var u = await unidades.AbrirAsync(true, ct);
                var completo = true;
                foreach (var o in publicador.Suscriptores) completo &= await u.Eventos.EntregadoAsync(e.Id, o.Nombre);
                if (completo) { await u.Eventos.MarcarProcesadoAsync(e.Id, reloj.Ahora); await u.ConfirmarAsync(ct); }
            }
        }
        await using var consulta = await unidades.AbrirAsync(false, ct);
        metricas.Sincronizar(await consulta.Eventos.MetricasAsync());
        var finales = await consulta.Eventos.ListarAsync();
        return new(finales.Count(x => x.Procesado is not null), finales.Count(x => x.Procesado is null));
    }
}
