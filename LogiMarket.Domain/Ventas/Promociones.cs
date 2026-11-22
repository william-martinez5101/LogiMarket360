using LogiMarket.Domain.Clientes;

namespace LogiMarket.Domain.Ventas;

public sealed record ContextoPromocion(CategoriaCliente Categoria, int Unidades, decimal Subtotal);
public interface IDescuento
{
    string Nombre { get; }
    int Prioridad { get; }
    bool Aplica(ContextoPromocion contexto);
    decimal Tasa { get; }
}
public sealed class Mayorista15 : IDescuento
{
    public string Nombre => "MAYORISTA15";
    public int Prioridad => 100;
    public decimal Tasa => .15m;
    public bool Aplica(ContextoPromocion c) => c.Unidades >= 10;
}
public sealed class Vip10 : IDescuento
{
    public string Nombre => "VIP10";
    public int Prioridad => 50;
    public decimal Tasa => .10m;
    public bool Aplica(ContextoPromocion c) => c.Categoria == CategoriaCliente.Vip;
}
public sealed class SinDescuento : IDescuento
{
    public string Nombre => "SIN_DESCUENTO";
    public int Prioridad => 0;
    public decimal Tasa => 0;
    public bool Aplica(ContextoPromocion c) => true;
}
public sealed record Valoracion(decimal Subtotal, decimal Descuento, decimal Tarifa, decimal Total, string Promocion, int Unidades);
public sealed class CalculadoraPedido(IEnumerable<IDescuento> politicas)
{
    private readonly IDescuento[] _politicas = politicas.OrderByDescending(x => x.Prioridad).ToArray();
    public Valoracion Calcular(CategoriaCliente categoria, IReadOnlyList<LineaPedido> lineas, decimal tarifa)
    {
        if (tarifa < 0 || lineas.Count == 0) throw new InvalidOperationException("Pedido sin lineas o tarifa invalida.");
        var subtotal = lineas.Sum(x => x.Importe);
        var unidades = lineas.Aggregate(0, (n, l) => checked(n + l.Unidades));
        var contexto = new ContextoPromocion(categoria, unidades, subtotal);
        var politica = _politicas.FirstOrDefault(p => p.Aplica(contexto))
            ?? throw new InvalidOperationException("No existe politica aplicable.");
        if (politica.Tasa < 0 || politica.Tasa > 1) throw new InvalidOperationException("Tasa invalida.");
        var descuento = decimal.Round(subtotal * politica.Tasa, 2, MidpointRounding.AwayFromZero);
        return new(subtotal, descuento, tarifa, subtotal - descuento + tarifa, politica.Nombre, unidades);
    }
}
