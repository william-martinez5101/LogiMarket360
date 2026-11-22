using LogiMarket.Application.Contratos;
using LogiMarket.Application.Seguridad;

namespace LogiMarket.Application.Pedidos;

public sealed class ConfirmacionPedidoFacade(IUnidadTrabajoFactory unidades, SesionUsuario sesion,
    CadenaConfirmacion cadena, IPasarelaPago pasarela, IReloj reloj, IDemostracion demo, IIntentosCobro intentos)
{
    public async Task<PagoRegistrado> ConfirmarAsync(Guid pedidoId, string clave, CancellationToken ct = default)
    {
        Permisos.Exigir(sesion, Rol.VENTAS);
        if (string.IsNullOrWhiteSpace(clave) || clave.Length > 120) throw new ArgumentException("Clave de idempotencia obligatoria, maximo 120 caracteres.");
        await using var u = await unidades.AbrirAsync(true, ct);
        // La unidad ya posee el bloqueo de escritura de SQLite ANTES de leer existencias o cobrar.
        var previo = await u.Operaciones.PagoPorClaveAsync(clave);
        if (previo is not null)
        {
            if (previo.PedidoId != pedidoId) throw new InvalidOperationException("La clave ya pertenece a otro pedido.");
            return previo;
        }
        var pedido = await u.Pedidos.BuscarAsync(pedidoId) ?? throw new InvalidOperationException("Pedido inexistente.");
        var confirmado = (await u.Operaciones.PagosAsync()).SingleOrDefault(x => x.PedidoId == pedidoId);
        if (confirmado is not null)
        {
            await intentos.PrepararAsync(pedidoId, clave, confirmado.Importe, ct);
            return confirmado;
        }
        var clientes = await u.Clientes.ListarAsync(); var productos = await u.Catalogo.ProductosAsync();
        var rechazo = cadena.Evaluar(new(clientes.SingleOrDefault(x => x.Id == pedido.ClienteId), pedido.Lineas, productos, sesion, pedido.Estado));
        if (rechazo is not null) throw new InvalidOperationException($"{rechazo.Codigo}: {rechazo.Mensaje}");
        var intento = await intentos.PrepararAsync(pedidoId, clave, pedido.Valores.Total, ct);
        var cobro = intento.Aprobacion ?? await pasarela.CobrarAsync(pedido.Valores.Total, clave, ct);
        if (!cobro.Aprobado) throw new InvalidOperationException($"Cobro rechazado: {cobro.Mensaje}");
        var pago = new PagoRegistrado(Guid.NewGuid(), pedidoId, clave, cobro.Referencia, pedido.Valores.Total, cobro.Proveedor, reloj.Ahora);
        await u.Operaciones.AgregarPagoAsync(pago);
        foreach (var hoja in pedido.Fisicos())
        {
            var producto = productos.Single(x => x.Sku == hoja.Sku); producto.DescontarStock(hoja.Cantidad);
            await u.Catalogo.GuardarProductoAsync(producto);
            await u.Operaciones.AgregarMovimientoAsync(new(Guid.NewGuid(), pedidoId, hoja.Sku, -hoja.Cantidad, "VENTA", pago.Id.ToString(), reloj.Ahora));
        }
        pedido.Pagar(reloj.Ahora, sesion.Exigir().Usuario); await u.Pedidos.GuardarAsync(pedido);
        await u.Eventos.AgregarAsync(new(Guid.NewGuid(), pedidoId, "PedidoPagado", pago.Importe, demo.Canal, reloj.Ahora));
        await u.ConfirmarAsync(ct); return pago;
    }
}
