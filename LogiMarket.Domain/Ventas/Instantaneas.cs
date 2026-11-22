using LogiMarket.Domain.Catalogo;

namespace LogiMarket.Domain.Ventas;

// Instantanea profunda: el arbol y las hojas no conservan referencias al catalogo mutable.
public sealed class NodoInstantanea
{
    public string Codigo { get; }
    public string Nombre { get; }
    public int Cantidad { get; }
    public decimal Precio { get; }
    public decimal PesoKg { get; }
    public IReadOnlyList<NodoInstantanea> Hijos { get; }
    public NodoInstantanea(string codigo, string nombre, int cantidad, decimal precio, decimal pesoKg, IReadOnlyList<NodoInstantanea> hijos)
    {
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre) || precio <= 0 || pesoKg < 0)
            throw new ArgumentException("Instantanea de catalogo invalida.");
        Codigo = codigo; Nombre = nombre; Cantidad = Cantidades.Validar(cantidad); Precio = precio; PesoKg = pesoKg;
        Hijos = hijos.Select(x => x.Copiar()).ToList().AsReadOnly();
    }
    public NodoInstantanea Copiar() => new(Codigo, Nombre, Cantidad, Precio, PesoKg, Hijos);
    public static NodoInstantanea Capturar(IElementoCatalogo elemento, int cantidad = 1)
        => elemento is ProductoSimple p
            ? new(p.Sku, p.Nombre, cantidad, p.Precio, p.PesoKg, [])
            : new(elemento.Codigo, elemento.Nombre, cantidad, elemento.CalcularPrecio(), 0,
                ((KitCompuesto)elemento).Componentes.Select(c => Capturar(c.Elemento, c.Cantidad)).ToList());
    public IEnumerable<ComponenteFisico> Expandir(int multiplicador = 1)
    {
        var total = Cantidades.Multiplicar(multiplicador, Cantidad);
        if (Hijos.Count == 0) yield return new(Codigo, Nombre, total, Precio, PesoKg);
        else foreach (var hijo in Hijos) foreach (var hoja in hijo.Expandir(total)) yield return hoja;
    }
}
public sealed record ComponenteFisico(string Sku, string Nombre, int Cantidad, decimal Precio, decimal PesoKg);

public sealed class LineaPedido
{
    public NodoInstantanea Elemento { get; }
    public int Cantidad { get; }
    public string Codigo => Elemento.Codigo;
    public decimal PrecioUnitario => Elemento.Precio;
    public decimal Importe => PrecioUnitario * Cantidad;
    public int Unidades => Fisicos().Aggregate(0, (n, c) => checked(n + c.Cantidad));
    public LineaPedido(NodoInstantanea elemento, int cantidad)
    { Elemento = elemento.Copiar(); Cantidad = Cantidades.Validar(cantidad); }
    public LineaPedido Copiar(int? cantidad = null) => new(Elemento, cantidad ?? Cantidad);
    public IEnumerable<ComponenteFisico> Fisicos() => Elemento.Expandir(Cantidad);
}
