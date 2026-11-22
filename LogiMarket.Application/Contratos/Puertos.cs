using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Application.Contratos;

public enum Rol { ADMIN, VENTAS, BODEGA, CONSULTA }
public sealed record Identidad(Guid Id, string Usuario, Rol Rol);
public sealed record UsuarioAutenticable(Guid Id, string Nombre, Rol Rol, string Hash);
public interface IUsuarios { Task<UsuarioAutenticable?> BuscarAsync(string nombre); }
public interface IHashContrasena { string Crear(string clave); bool Verificar(string clave, string hash); }
public interface IReloj { DateTimeOffset Ahora { get; } }

public interface IUnidadTrabajoFactory { Task<IUnidadTrabajo> AbrirAsync(bool escritura, CancellationToken ct = default); }
public interface IUnidadTrabajo : IAsyncDisposable
{
    ICatalogoRepository Catalogo { get; }
    IClienteRepository Clientes { get; }
    IPedidoRepository Pedidos { get; }
    IOperacionRepository Operaciones { get; }
    IEventoRepository Eventos { get; }
    IBitacoraRepository Bitacora { get; }
    Task ConfirmarAsync(CancellationToken ct = default);
}
public interface ICatalogoRepository
{
    Task<IReadOnlyList<ProductoSimple>> ProductosAsync();
    Task<IReadOnlyList<KitCompuesto>> KitsAsync();
    Task GuardarProductoAsync(ProductoSimple producto);
    Task EliminarProductoAsync(string sku);
    Task GuardarKitAsync(string codigo, string nombre, IReadOnlyList<ComposicionKit> componentes);
    Task EliminarKitAsync(string codigo);
}
public sealed record ComposicionKit(string Codigo, int Cantidad);
public interface IClienteRepository
{
    Task<IReadOnlyList<Cliente>> ListarAsync();
    Task GuardarAsync(Cliente cliente);
}
public interface IPedidoRepository
{
    Task<Pedido?> BuscarAsync(Guid id);
    Task<IReadOnlyList<Pedido>> ListarAsync();
    Task GuardarAsync(Pedido pedido);
}
public sealed record PagoRegistrado(Guid Id, Guid PedidoId, string Clave, string Referencia, decimal Importe, string Proveedor, DateTimeOffset Fecha);
public sealed record MovimientoInventario(Guid Id, Guid? PedidoId, string Sku, int Cantidad, string Motivo, string Referencia, DateTimeOffset Fecha);
public sealed record DespachoRegistrado(Guid PedidoId, string Guia, DateTimeOffset Fecha, DateTimeOffset Estimada);
public sealed record DevolucionRegistrada(Guid Id, Guid PedidoId, string Motivo, decimal Reembolso, string Referencia, DateTimeOffset Fecha);
public interface IOperacionRepository
{
    Task<PagoRegistrado?> PagoPorClaveAsync(string clave);
    Task<IReadOnlyList<PagoRegistrado>> PagosAsync();
    Task AgregarPagoAsync(PagoRegistrado pago);
    Task AgregarMovimientoAsync(MovimientoInventario movimiento);
    Task<IReadOnlyList<MovimientoInventario>> MovimientosAsync();
    Task AgregarDespachoAsync(DespachoRegistrado despacho);
    Task<IReadOnlyList<DespachoRegistrado>> DespachosAsync();
    Task AgregarDevolucionAsync(DevolucionRegistrada devolucion);
    Task<IReadOnlyList<DevolucionRegistrada>> DevolucionesAsync();
}
public sealed record EventoNegocio(Guid Id, Guid PedidoId, string Tipo, decimal Importe, string Canal, DateTimeOffset Fecha);
public sealed record EventoPendiente(EventoNegocio Evento, int Intentos, DateTimeOffset? Procesado, string? Error);
public sealed record AvisoRegistrado(Guid EventoId, string Canal, string Mensaje, DateTimeOffset Fecha);
public sealed record MetricaRegistrada(Guid EventoId, string Tipo, decimal Importe);
public interface IEventoRepository
{
    Task AgregarAsync(EventoNegocio evento);
    Task<IReadOnlyList<EventoPendiente>> ListarAsync();
    Task<bool> EntregadoAsync(Guid eventoId, string suscriptor);
    Task MarcarEntregaAsync(Guid eventoId, string suscriptor);
    Task MarcarProcesadoAsync(Guid eventoId, DateTimeOffset fecha);
    Task RegistrarErrorAsync(Guid eventoId, string error);
    Task AgregarAvisoAsync(AvisoRegistrado aviso);
    Task<IReadOnlyList<AvisoRegistrado>> AvisosAsync();
    Task AgregarMetricaAsync(MetricaRegistrada metrica);
    Task<IReadOnlyList<MetricaRegistrada>> MetricasAsync();
}
public sealed record EntradaBitacora(Guid Id, string Tipo, string Correlacion, string Mensaje, DateTimeOffset Fecha);
public interface IBitacoraRepository
{
    Task AgregarAsync(EntradaBitacora entrada);
    Task<IReadOnlyList<EntradaBitacora>> ListarAsync();
}
public interface IBitacoraTecnica
{
    void Registrar(string tipo, string correlacion, string mensaje);
    IReadOnlyList<EntradaBitacora> Leer();
}
public sealed record RespuestaCobro(bool Aprobado, string Referencia, string Proveedor, string Mensaje);
public sealed record IntentoCobro(Guid PedidoId, decimal Importe, RespuestaCobro? Aprobacion);
// La identidad durable es el pedido. Las claves de reintento quedan reservadas a su propietario.
public interface IIntentosCobro
{
    Task<IntentoCobro> PrepararAsync(Guid pedidoId, string clave, decimal importe, IReadOnlyCollection<string> referenciasConfirmadas, CancellationToken ct = default);
    Task<IntentoCobro?> BuscarAsync(Guid pedidoId, CancellationToken ct = default);
}
// Contrato comun: monto positivo con dos decimales y clave no vacia; mismos errores de entrada.
public interface IPasarelaPago { Task<RespuestaCobro> CobrarAsync(decimal monto, string clave, CancellationToken ct = default); }
public interface INotificador { Task NotificarAsync(EventoNegocio evento, IUnidadTrabajo unidad); }
public abstract class CreadorNotificacion
{
    public abstract string Canal { get; }
    public abstract INotificador CrearNotificador();
    public Task EnviarAsync(EventoNegocio evento, IUnidadTrabajo unidad) => CrearNotificador().NotificarAsync(evento, unidad);
}
public interface IObservadorEvento
{
    string Nombre { get; }
    Task AplicarAsync(EventoNegocio evento, IUnidadTrabajo unidad);
}
public sealed record DocumentoEmitido(string Contenido, string? Ruta);
public interface ISalidaDocumento { Task<DocumentoEmitido> EmitirAsync(string nombre, string contenido); }
public interface IDemostracion
{
    string Proveedor { get; set; }
    string Canal { get; set; }
    bool FallarCobro { get; set; }
    bool FallarNotificacion { get; set; }
    bool FallarPersistencia { get; set; }
}
