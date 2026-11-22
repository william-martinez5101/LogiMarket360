using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiMarket.Domain.Catalogo;

public sealed class ProductoSimple : IElementoCatalogo
{
    public void Aceptar(IVisitanteCatalogo visitante, int cantidad = 1)
        => visitante.Visitar(this, Cantidades.Validar(cantidad));

    public Guid Id { get; private set; }

    public string Sku { get; private set; } = string.Empty;
    public string Codigo => Sku;

    public string Nombre { get; private set; } = string.Empty;

    public decimal Precio { get; private set; }

    public decimal PesoKg { get; private set; }

    public int Stock { get; private set; }

    // Constructor reservado para la persistencia.
    private ProductoSimple()
    {
    }

    public ProductoSimple(
        string sku,
        string nombre,
        decimal precio,
        decimal pesoKg,
        int stockInicial)
    {
        var skuValidado = TextoObligatorio(sku, nameof(sku))
            .ToUpperInvariant();

        var nombreValidado = TextoObligatorio(nombre, nameof(nombre));

        ValidarPrecioYPeso(precio, pesoKg);

        if (stockInicial < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stockInicial),
                "El stock inicial no puede ser negativo.");
        }

        Id = Guid.NewGuid();
        Sku = skuValidado;
        Nombre = nombreValidado;
        Precio = precio;
        PesoKg = pesoKg;
        Stock = stockInicial;
    }

    public void ActualizarDatos(
        string nombre,
        decimal precio,
        decimal pesoKg)
    {
        // Validar todo antes de modificar evita cambios parciales.
        var nombreValidado = TextoObligatorio(nombre, nameof(nombre));
        ValidarPrecioYPeso(precio, pesoKg);

        Nombre = nombreValidado;
        Precio = precio;
        PesoKg = pesoKg;
    }

    public void DescontarStock(int cantidad)
    {
        ValidarCantidad(cantidad);

        if (cantidad > Stock)
        {
            throw new InvalidOperationException(
                $"Stock insuficiente para {Sku}.");
        }

        Stock -= cantidad;
    }

    public void ReintegrarStock(int cantidad)
    {
        ValidarCantidad(cantidad);

        // Evitar que un desbordamiento produzca existencias invalidas.
        Stock = checked(Stock + cantidad);
    }

    // Composite: el producto es una hoja del catalogo.
    public decimal CalcularPrecio()
    {
        return Precio;
    }

    private static string TextoObligatorio(
        string valor,
        string parametro)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                "El campo es obligatorio.",
                parametro);
        }

        return valor.Trim();
    }

    private static void ValidarPrecioYPeso(
        decimal precio,
        decimal pesoKg)
    {
        if (precio <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(precio),
                "El precio debe ser mayor que cero.");
        }

        if (pesoKg < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pesoKg),
                "El peso no puede ser negativo.");
        }
    }

    private static void ValidarCantidad(int cantidad)
    {
        if (cantidad <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cantidad),
                "La cantidad debe ser mayor que cero.");
        }
    }
}
