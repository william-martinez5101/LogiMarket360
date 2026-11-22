using System.Globalization;
using LogiMarket.Application.Contratos;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Application.Documentos;

public abstract class Comprobante(ISalidaDocumento salida)
{
    protected abstract string Tipo { get; }
    protected abstract string Cuerpo(Pedido pedido, DevolucionRegistrada? devolucion);
    public Task<DocumentoEmitido> EmitirAsync(Pedido pedido, DevolucionRegistrada? devolucion = null)
        => salida.EmitirAsync($"{Tipo}-{pedido.Id:N}", $"LOGIMARKET 360 - COMPROBANTE INTERNO {Tipo}\r\nSimulacion sin validez tributaria\r\nPedido: {pedido.Id}\r\nCliente: {pedido.ClienteNombre}\r\n{Cuerpo(pedido, devolucion)}");
    protected static string D(decimal importe) => importe.ToString("F2", CultureInfo.InvariantCulture);
}
public sealed class ComprobanteVenta(ISalidaDocumento salida) : Comprobante(salida)
{
    protected override string Tipo => "VENTA";
    protected override string Cuerpo(Pedido p, DevolucionRegistrada? d)
    {
        if (p.Estado is EstadoPedido.BORRADOR or EstadoPedido.VALIDADO or EstadoPedido.CANCELADO) throw new InvalidOperationException("El comprobante de venta requiere un pedido pagado.");
        return string.Join("\r\n", p.Lineas.Select(l => $"{l.Codigo} x{l.Cantidad} | USD {D(l.PrecioUnitario)} | USD {D(l.Importe)}"))
            + $"\r\nSubtotal USD {D(p.Valores.Subtotal)}\r\nDescuento {p.Valores.Promocion} USD {D(p.Valores.Descuento)}\r\nEntrega USD {D(p.Valores.Tarifa)}\r\nTOTAL USD {D(p.Valores.Total)}\r\nEstado {p.Estado}";
    }
}
public sealed class ComprobanteDevolucion(ISalidaDocumento salida) : Comprobante(salida)
{
    protected override string Tipo => "DEVOLUCION";
    protected override string Cuerpo(Pedido p, DevolucionRegistrada? d)
    {
        if (d is null || d.PedidoId != p.Id || !p.Devuelto) throw new InvalidOperationException("No existe devolucion aprobada para este pedido.");
        return $"Motivo: {d.Motivo}\r\nREEMBOLSO USD {D(d.Reembolso)}\r\nSin tarifa de envio\r\nReferencia: {d.Referencia}";
    }
}
