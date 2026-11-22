using System.Text.Json;
using LogiMarket.Application.Contratos;
using LogiMarket.Application.Seguridad;

namespace LogiMarket.Application.Pedidos;

public interface IComando<T>
{
    string Entrada { get; }
    Task<T> EjecutarAsync(CancellationToken ct = default);
}
public sealed record ConfirmarPedidoCommand(ConfirmacionPedidoFacade Receptor, Guid PedidoId, string Clave) : IComando<PagoRegistrado>
{
    public string Entrada => $"Pedido={PedidoId}; clave={Clave}";
    public Task<PagoRegistrado> EjecutarAsync(CancellationToken ct = default) => Receptor.ConfirmarAsync(PedidoId, Clave, ct);
}
public sealed record CancelarPedidoCommand(PedidosServicio Receptor, Guid PedidoId) : IComando<string>
{
    public string Entrada => PedidoId.ToString();
    public Task<string> EjecutarAsync(CancellationToken ct = default) => Receptor.CancelarAsync(PedidoId);
}
public sealed record DespacharPedidoCommand(PedidosServicio Receptor, Guid PedidoId) : IComando<DespachoRegistrado>
{
    public string Entrada => PedidoId.ToString();
    public Task<DespachoRegistrado> EjecutarAsync(CancellationToken ct = default) => Receptor.DespacharAsync(PedidoId);
}
public sealed record ProcesarDevolucionCommand(DevolucionesServicio Receptor, SolicitudDevolucion Solicitud) : IComando<DevolucionRegistrada>
{
    public string Entrada => JsonSerializer.Serialize(Solicitud);
    public Task<DevolucionRegistrada> EjecutarAsync(CancellationToken ct = default) => Receptor.ProcesarAsync(Solicitud);
}
public sealed class InvocadorComandos(IUnidadTrabajoFactory unidades, IReloj reloj, SesionUsuario sesion, IBitacoraTecnica tecnica)
{
    public async Task<T> EjecutarAsync<T>(IComando<T> comando, CancellationToken ct = default)
    {
        var identidad = sesion.Exigir(); var correlacion = Guid.NewGuid().ToString();
        var nombre = comando.GetType().Name;
        async Task Registrar(string fase, string resultado)
        {
            var e = new EntradaBitacora(Guid.NewGuid(), "COMMAND", correlacion, $"{nombre}; {identidad.Usuario}; {fase}; {comando.Entrada}; {resultado}", reloj.Ahora);
            tecnica.Registrar(e.Tipo, e.Correlacion, e.Mensaje);
            await using var u = await unidades.AbrirAsync(true, ct); await u.Bitacora.AgregarAsync(e); await u.ConfirmarAsync(ct);
        }
        await Registrar("INICIO", "");
        try
        {
            var resultado = await comando.EjecutarAsync(ct);
            // La operacion ya esta confirmada: un fallo del log no convierte un exito en rechazo.
            try { await Registrar("FIN", JsonSerializer.Serialize(resultado)); }
            catch (Exception e) { tecnica.Registrar("COMMAND_LOG_PENDIENTE", correlacion, e.Message); }
            return resultado;
        }
        catch (Exception ex)
        {
            try { await Registrar("ERROR", ex.Message); } catch (Exception logError) { tecnica.Registrar("COMMAND_LOG_ERROR", correlacion, logError.Message); }
            throw;
        }
    }
}
