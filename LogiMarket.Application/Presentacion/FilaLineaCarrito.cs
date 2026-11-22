using LogiMarket.Domain.Ventas;

namespace LogiMarket.Application.Presentacion;

// Linea conserva la posicion del modelo durante este render, aunque se ordene la proyeccion.
public sealed record FilaLineaCarrito(int Linea, string Articulo, int Cantidad, decimal Precio, decimal Importe, int UnidadesFisicas)
{
    public static IEnumerable<FilaLineaCarrito> Crear(IReadOnlyList<LineaPedido> lineas)
        => lineas.Select((l, i) => new FilaLineaCarrito(i + 1, l.Codigo, l.Cantidad, l.PrecioUnitario, l.Importe, l.Unidades));
    public int IndiceEn(IReadOnlyList<LineaPedido> lineas)
    {
        var indice = Linea - 1;
        if (indice < 0 || indice >= lineas.Count || lineas[indice].Codigo != Articulo
            || lineas[indice].Cantidad != Cantidad || lineas[indice].PrecioUnitario != Precio)
            throw new InvalidOperationException("La linea seleccionada cambio. Vuelva a seleccionarla.");
        return indice;
    }
}
