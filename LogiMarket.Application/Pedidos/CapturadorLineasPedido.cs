using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Application.Pedidos;

// Los datos del llamador son una solicitud. Solo el catalogo y las instantaneas
// previamente persistidas por el servidor pueden aportar precios y composicion.
internal sealed class CapturadorLineasPedido(IEnumerable<IElementoCatalogo> catalogo, IEnumerable<LineaPedido> historicas)
{
    private readonly Dictionary<string, IElementoCatalogo> _catalogo = catalogo.ToDictionary(x => x.Codigo, StringComparer.Ordinal);
    private readonly List<LineaPedido> _historicas = historicas.ToList();

    public LineaPedido Capturar(LineaPedido solicitada)
    {
        var indice = _historicas.FindIndex(x => Iguales(x.Elemento, solicitada.Elemento));
        if (indice >= 0)
        {
            var original = _historicas[indice]; _historicas.RemoveAt(indice);
            return original.Copiar(solicitada.Cantidad);
        }
        if (!_catalogo.TryGetValue(solicitada.Codigo, out var elemento))
            throw new InvalidOperationException($"El articulo {solicitada.Codigo} no pertenece al catalogo actual.");
        return new(NodoInstantanea.Capturar(elemento), solicitada.Cantidad);
    }
    private static bool Iguales(NodoInstantanea a, NodoInstantanea b)
        => a.Codigo == b.Codigo && a.Nombre == b.Nombre && a.Cantidad == b.Cantidad
        && a.Precio == b.Precio && a.PesoKg == b.PesoKg && a.Hijos.Count == b.Hijos.Count
        && a.Hijos.Zip(b.Hijos).All(x => Iguales(x.First, x.Second));
}
