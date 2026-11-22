using LogiMarket.Application.Contratos;

namespace LogiMarket.Infrastructure.Eventos;

public abstract class NotificadorLocal(IDemostracion demo, IReloj reloj, IBitacoraTecnica bitacora) : INotificador
{
    protected abstract string Canal { get; }
    public async Task NotificarAsync(EventoNegocio e, IUnidadTrabajo u)
    {
        if (demo.FallarNotificacion) throw new IOException($"Fallo simulado de notificador {Canal}.");
        var mensaje = $"{Canal}: {e.Tipo}; pedido {e.PedidoId}; importe USD {e.Importe:F2}. Aviso local de demostracion.";
        await u.Eventos.AgregarAvisoAsync(new(e.Id, Canal, mensaje, reloj.Ahora));
        bitacora.Registrar("NOTIFICACION", e.Id.ToString(), mensaje);
    }
}
public sealed class NotificadorEmail(IDemostracion demo, IReloj reloj, IBitacoraTecnica bitacora) : NotificadorLocal(demo, reloj, bitacora)
{ protected override string Canal => "EMAIL"; }
public sealed class NotificadorSms(IDemostracion demo, IReloj reloj, IBitacoraTecnica bitacora) : NotificadorLocal(demo, reloj, bitacora)
{ protected override string Canal => "SMS"; }
public sealed class CreadorEmail(IDemostracion demo, IReloj reloj, IBitacoraTecnica bitacora) : CreadorNotificacion
{
    public override string Canal => "EMAIL";
    public override INotificador CrearNotificador() => new NotificadorEmail(demo, reloj, bitacora);
}
public sealed class CreadorSms(IDemostracion demo, IReloj reloj, IBitacoraTecnica bitacora) : CreadorNotificacion
{
    public override string Canal => "SMS";
    public override INotificador CrearNotificador() => new NotificadorSms(demo, reloj, bitacora);
}
