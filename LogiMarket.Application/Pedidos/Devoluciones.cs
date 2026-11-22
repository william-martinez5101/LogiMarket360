using LogiMarket.Application.Contratos;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Application.Pedidos;

public sealed record SolicitudDevolucion(Guid PedidoId, string Motivo, bool SinUso);
public abstract class ProcesarDevolucion
{
    public abstract string Motivo { get; }
    protected abstract int PlazoDias { get; }
    protected abstract void Inspeccionar(bool sinUso);
    // Template Method: el orden no puede ser sustituido por las variantes.
    public async Task<DevolucionRegistrada> EjecutarAsync(Pedido p, SolicitudDevolucion solicitud, IUnidadTrabajo u,
        DateTimeOffset ahora, string canal)
    {
        if (p.Estado != EstadoPedido.ENTREGADO || p.Devuelto || p.FechaEntrega is null)
            throw new InvalidOperationException("Solo se devuelve un pedido ENTREGADO y no devuelto.");
        var dias = (ahora.UtcDateTime.Date - p.FechaEntrega.Value.UtcDateTime.Date).Days;
        if (dias < 0 || dias > PlazoDias) throw new InvalidOperationException($"{Motivo}: plazo de {PlazoDias} dias excedido.");
        Inspeccionar(solicitud.SinUso);
        var reembolso = p.Valores.Subtotal - p.Valores.Descuento;
        var devolucion = new DevolucionRegistrada(Guid.NewGuid(), p.Id, Motivo, reembolso, $"REEMBOLSO-{p.Id:N}", ahora);
        var productos = await u.Catalogo.ProductosAsync();
        foreach (var hoja in p.Fisicos())
        {
            var producto = productos.Single(x => x.Sku == hoja.Sku); producto.ReintegrarStock(hoja.Cantidad);
            await u.Catalogo.GuardarProductoAsync(producto);
            await u.Operaciones.AgregarMovimientoAsync(new(Guid.NewGuid(), p.Id, hoja.Sku, hoja.Cantidad, "DEVOLUCION", devolucion.Id.ToString(), ahora));
        }
        p.RegistrarDevolucion(); await u.Operaciones.AgregarDevolucionAsync(devolucion); await u.Pedidos.GuardarAsync(p);
        await u.Eventos.AgregarAsync(new(Guid.NewGuid(), p.Id, "DevolucionAceptada", reembolso, canal, ahora));
        return devolucion;
    }
}
public sealed class DevolucionDefecto : ProcesarDevolucion
{
    public override string Motivo => "DEFECTO"; protected override int PlazoDias => 30;
    protected override void Inspeccionar(bool sinUso) { }
}
public sealed class DevolucionArrepentimiento : ProcesarDevolucion
{
    public override string Motivo => "ARREPENTIMIENTO"; protected override int PlazoDias => 7;
    protected override void Inspeccionar(bool sinUso)
    { if (!sinUso) throw new InvalidOperationException("ARREPENTIMIENTO requiere producto sin uso."); }
}
public sealed class DevolucionesServicio(IUnidadTrabajoFactory unidades, SesionUsuario sesion, IReloj reloj,
    IEnumerable<ProcesarDevolucion> politicas, IDemostracion demo)
{
    public async Task<DevolucionRegistrada> ProcesarAsync(SolicitudDevolucion solicitud)
    {
        Permisos.Exigir(sesion, Rol.VENTAS); await using var u = await unidades.AbrirAsync(true);
        var p = await PedidosServicio.ExigirPedido(u, solicitud.PedidoId);
        var politica = politicas.SingleOrDefault(x => x.Motivo == solicitud.Motivo) ?? throw new ArgumentException("Motivo desconocido.");
        var resultado = await politica.EjecutarAsync(p, solicitud, u, reloj.Ahora, demo.Canal);
        await u.ConfirmarAsync(); return resultado;
    }
}
