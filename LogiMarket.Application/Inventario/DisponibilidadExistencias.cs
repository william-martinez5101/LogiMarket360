using LogiMarket.Application.Contratos;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Application.Inventario;

public sealed record DisponibilidadSku(string Sku, int Fisico, int Comprometido, int Disponible);

// La aceptacion durable y las lineas inmutables de VALIDADO constituyen la reserva.
// No depende del commit que puede fallar ni de una liberacion posterior a ese commit.
public sealed class DisponibilidadExistencias(IIntentosCobro intentos)
{
    public async Task<IReadOnlyList<DisponibilidadSku>> CalcularAsync(IReadOnlyList<ProductoSimple> productos,
        IReadOnlyList<Pedido> pedidos, IReadOnlyList<PagoRegistrado> pagos, Guid? pedidoPropio = null, CancellationToken ct = default)
    {
        var comprometido = new Dictionary<string, int>(StringComparer.Ordinal);
        var porPedido = pedidos.ToDictionary(x => x.Id); var pagados = pagos.ToDictionary(x => x.PedidoId);
        foreach (var aceptacion in await intentos.AceptadosAsync(ct))
        {
            if (pagados.TryGetValue(aceptacion.PedidoId, out var pago))
            {
                if (pago.Referencia != aceptacion.Aprobacion!.Referencia || pago.Importe != aceptacion.Importe)
                    throw new InvalidOperationException("El pago no coincide con su aceptacion; requiere conciliacion.");
                continue; // Ya descuento stock dentro del mismo commit que registro el pago.
            }
            if (!porPedido.TryGetValue(aceptacion.PedidoId, out var pedido) || pedido.Estado != EstadoPedido.VALIDADO
                || pedido.Valores.Total != aceptacion.Importe)
                throw new InvalidOperationException("Aceptacion pendiente sin pedido VALIDADO compatible; requiere conciliacion.");
            if (pedido.Id == pedidoPropio) continue; // El propietario puede consumir sus unidades, sin restarlas dos veces.
            foreach (var hoja in pedido.Fisicos())
                comprometido[hoja.Sku] = checked(comprometido.GetValueOrDefault(hoja.Sku) + hoja.Cantidad);
        }
        if (comprometido.Keys.Any(sku => !productos.Any(p => p.Sku == sku)))
            throw new InvalidOperationException("Un SKU comprometido ya no existe; requiere conciliacion.");
        return productos.Select(p => new DisponibilidadSku(p.Sku, p.Stock, comprometido.GetValueOrDefault(p.Sku),
            Math.Max(0, p.Stock - comprometido.GetValueOrDefault(p.Sku)))).ToList();
    }
}
