using LogiMarket.Application.Contratos;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Application.Pedidos;

public sealed record RechazoValidacion(string Codigo, string Mensaje);
public sealed record ContextoValidacion(Cliente? Cliente, IReadOnlyList<LineaPedido> Lineas,
    IReadOnlyList<ProductoSimple> Productos, SesionUsuario Sesion, EstadoPedido Estado,
    IReadOnlyDictionary<string, int>? Disponibles = null);
public abstract class ValidadorPedido
{
    private ValidadorPedido? _siguiente;
    public ValidadorPedido SeguidoDe(ValidadorPedido siguiente) { _siguiente = siguiente; return siguiente; }
    public RechazoValidacion? Evaluar(ContextoValidacion c) => Comprobar(c) ?? _siguiente?.Evaluar(c);
    protected abstract RechazoValidacion? Comprobar(ContextoValidacion c);
}
public sealed class ClienteActivoValidador : ValidadorPedido
{
    protected override RechazoValidacion? Comprobar(ContextoValidacion c) => c.Cliente is null || !c.Cliente.Activo
        ? new("CLIENTE", "Seleccione un cliente activo.") : null;
}
public sealed class LineasValidador : ValidadorPedido
{
    protected override RechazoValidacion? Comprobar(ContextoValidacion c) => c.Lineas.Count == 0 || c.Lineas.Any(x => x.Cantidad <= 0)
        ? new("LINEAS", "Agregue lineas con cantidades positivas.") : null;
}
public sealed class StockValidador : ValidadorPedido
{
    protected override RechazoValidacion? Comprobar(ContextoValidacion c)
    {
        foreach (var grupo in c.Lineas.SelectMany(l => l.Fisicos()).GroupBy(x => x.Sku).OrderBy(x => x.Key))
        {
            var cantidad = grupo.Aggregate(0, (n, x) => checked(n + x.Cantidad));
            var producto = c.Productos.SingleOrDefault(x => x.Sku == grupo.Key);
            var disponible = producto is null ? 0 : c.Disponibles is null ? producto.Stock
                : Math.Min(producto.Stock, c.Disponibles.GetValueOrDefault(grupo.Key));
            if (producto is null || disponible < cantidad)
                return new("STOCK", $"Stock insuficiente para {grupo.Key}: requiere {cantidad}, disponible {disponible}.");
        }
        return null;
    }
}
public sealed class CondicionesValidador : ValidadorPedido
{
    protected override RechazoValidacion? Comprobar(ContextoValidacion c)
    {
        if (c.Sesion.Actual?.Rol is not (Rol.ADMIN or Rol.VENTAS)) return new("PERMISO", "Cobro permitido solo a ADMIN/VENTAS.");
        return c.Estado != EstadoPedido.VALIDADO ? new("ESTADO", "El pedido debe estar VALIDADO antes del cobro.") : null;
    }
}
public sealed class CadenaConfirmacion
{
    private readonly ValidadorPedido _primero = new ClienteActivoValidador();
    public CadenaConfirmacion() => _primero.SeguidoDe(new LineasValidador()).SeguidoDe(new StockValidador()).SeguidoDe(new CondicionesValidador());
    public RechazoValidacion? Evaluar(ContextoValidacion contexto) => _primero.Evaluar(contexto);
}
