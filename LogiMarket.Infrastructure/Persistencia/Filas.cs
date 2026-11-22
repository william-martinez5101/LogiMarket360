using LogiMarket.Application.Contratos;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Infrastructure.Persistencia;

internal sealed class UsuarioFila { public Guid Id { get; set; } public string Nombre { get; set; } = ""; public Rol Rol { get; set; } public string Hash { get; set; } = ""; }
internal sealed class KitFila { public Guid Id { get; set; } public string Codigo { get; set; } = ""; public string Nombre { get; set; } = ""; public List<KitComponenteFila> Componentes { get; set; } = []; }
internal sealed class KitComponenteFila { public Guid Id { get; set; } public Guid KitId { get; set; } public Guid? ProductoId { get; set; } public Guid? SubkitId { get; set; } public int Cantidad { get; set; } public int Orden { get; set; } }
internal sealed class PedidoFila
{
    public Guid Id { get; set; } public Guid ClienteId { get; set; } public string ClienteNombre { get; set; } = "";
    public CategoriaCliente Categoria { get; set; } public EstadoPedido Estado { get; set; }
    public TipoEntrega TipoEntrega { get; set; } public int Dias { get; set; } public string Guia { get; set; } = "";
    public decimal Subtotal { get; set; } public decimal Descuento { get; set; } public decimal Tarifa { get; set; }
    public decimal Total { get; set; } public string Promocion { get; set; } = ""; public int Unidades { get; set; }
    public DateTimeOffset Creado { get; set; } public DateTimeOffset? Entregado { get; set; } public bool Devuelto { get; set; }
    public long Version { get; set; } public List<DetalleFila> Detalles { get; set; } = []; public List<EstadoFila> Historial { get; set; } = [];
}
internal sealed class DetalleFila
{
    public Guid Id { get; set; } public Guid PedidoId { get; set; } public int Orden { get; set; }
    public string Codigo { get; set; } = ""; public int Cantidad { get; set; } public decimal Precio { get; set; }
    public string ArbolJson { get; set; } = ""; public List<FisicoFila> Fisicos { get; set; } = [];
}
internal sealed class FisicoFila
{
    public Guid Id { get; set; } public Guid DetalleId { get; set; } public string Sku { get; set; } = "";
    public string Nombre { get; set; } = ""; public int Cantidad { get; set; } public decimal Precio { get; set; } public decimal PesoKg { get; set; }
}
internal sealed class EstadoFila { public Guid Id { get; set; } public Guid PedidoId { get; set; } public int Orden { get; set; } public EstadoPedido? Desde { get; set; } public EstadoPedido Hasta { get; set; } public DateTimeOffset Fecha { get; set; } public string Actor { get; set; } = ""; }
internal sealed class PagoFila { public Guid Id { get; set; } public Guid PedidoId { get; set; } public string Clave { get; set; } = ""; public string Referencia { get; set; } = ""; public decimal Importe { get; set; } public string Proveedor { get; set; } = ""; public DateTimeOffset Fecha { get; set; } }
internal sealed class MovimientoFila { public Guid Id { get; set; } public Guid? PedidoId { get; set; } public string Sku { get; set; } = ""; public int Cantidad { get; set; } public string Motivo { get; set; } = ""; public string Referencia { get; set; } = ""; public DateTimeOffset Fecha { get; set; } }
internal sealed class DespachoFila { public Guid PedidoId { get; set; } public string Guia { get; set; } = ""; public DateTimeOffset Fecha { get; set; } public DateTimeOffset Estimada { get; set; } }
internal sealed class DevolucionFila { public Guid Id { get; set; } public Guid PedidoId { get; set; } public string Motivo { get; set; } = ""; public decimal Reembolso { get; set; } public string Referencia { get; set; } = ""; public DateTimeOffset Fecha { get; set; } }
internal sealed class EventoFila { public Guid Id { get; set; } public Guid PedidoId { get; set; } public string Tipo { get; set; } = ""; public decimal Importe { get; set; } public string Canal { get; set; } = ""; public DateTimeOffset Fecha { get; set; } public int Intentos { get; set; } public string? Error { get; set; } public DateTimeOffset? Procesado { get; set; } }
internal sealed class EntregaEventoFila { public Guid EventoId { get; set; } public string Suscriptor { get; set; } = ""; }
internal sealed class AvisoFila { public Guid EventoId { get; set; } public string Canal { get; set; } = ""; public string Mensaje { get; set; } = ""; public DateTimeOffset Fecha { get; set; } }
internal sealed class MetricaFila { public Guid EventoId { get; set; } public string Tipo { get; set; } = ""; public decimal Importe { get; set; } }
internal sealed class BitacoraFila { public Guid Id { get; set; } public string Tipo { get; set; } = ""; public string Correlacion { get; set; } = ""; public string Mensaje { get; set; } = ""; public DateTimeOffset Fecha { get; set; } }
