using System.Collections;

namespace LogiMarket.Domain.Catalogo;

public static class Cantidades
{
    public static int Validar(int cantidad) => cantidad > 0 ? cantidad
        : throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser positiva.");
    public static int Multiplicar(int a, int b) => checked(Validar(a) * Validar(b));
}

public sealed record HojaCatalogo(ProductoSimple Producto, int Cantidad);

// Iterator: pila propia de nodos pendientes, sin exponer las listas del Composite.
public sealed class IteradorFisico : IEnumerator<HojaCatalogo>
{
    private readonly IElementoCatalogo _raiz;
    private readonly int _cantidad;
    private readonly Stack<(IElementoCatalogo Nodo, int Cantidad)> _pendientes = new();
    private HojaCatalogo? _actual;
    public IteradorFisico(IElementoCatalogo raiz, int cantidad = 1)
    {
        _raiz = raiz ?? throw new ArgumentNullException(nameof(raiz));
        _cantidad = Cantidades.Validar(cantidad);
        Reset();
    }
    public HojaCatalogo Current => _actual ?? throw new InvalidOperationException("Iterador fuera del recorrido.");
    object IEnumerator.Current => Current;
    public bool MoveNext()
    {
        _actual = null;
        while (_pendientes.TryPop(out var item))
        {
            if (item.Nodo is ProductoSimple producto)
            {
                _actual = new(producto, item.Cantidad);
                return true;
            }
            var kit = (KitCompuesto)item.Nodo;
            if (kit.Componentes.Count == 0) throw new InvalidOperationException("Kit vacio.");
            for (var i = kit.Componentes.Count - 1; i >= 0; i--)
            {
                var hijo = kit.Componentes[i];
                _pendientes.Push((hijo.Elemento, Cantidades.Multiplicar(item.Cantidad, hijo.Cantidad)));
            }
        }
        return false;
    }
    public void Reset() { _pendientes.Clear(); _pendientes.Push((_raiz, _cantidad)); _actual = null; }
    public void Dispose() { _pendientes.Clear(); _actual = null; }
}

public interface IVisitanteCatalogo
{
    void Visitar(ProductoSimple producto, int cantidad);
    void Visitar(KitCompuesto kit, int cantidad);
}

public abstract class VisitanteRecursivo : IVisitanteCatalogo
{
    public abstract void Visitar(ProductoSimple producto, int cantidad);
    public void Visitar(KitCompuesto kit, int cantidad)
    {
        if (kit.Componentes.Count == 0) throw new InvalidOperationException("Kit vacio.");
        foreach (var hijo in kit.Componentes)
            hijo.Elemento.Aceptar(this, Cantidades.Multiplicar(cantidad, hijo.Cantidad));
    }
}

public sealed class DesgloseFisicoVisitor : VisitanteRecursivo
{
    private readonly Dictionary<string, int> _unidades = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, int> Unidades => new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(_unidades);
    public override void Visitar(ProductoSimple producto, int cantidad)
        => _unidades[producto.Sku] = checked(_unidades.GetValueOrDefault(producto.Sku) + cantidad);
}

public sealed class PesoLogisticoVisitor : VisitanteRecursivo
{
    public decimal PesoKg { get; private set; }
    public override void Visitar(ProductoSimple producto, int cantidad) => PesoKg += producto.PesoKg * cantidad;
}
