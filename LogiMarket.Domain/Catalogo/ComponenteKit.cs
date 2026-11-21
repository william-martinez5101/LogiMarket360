using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiMarket.Domain.Catalogo;

public sealed class ComponenteKit
{
    public IElementoCatalogo Elemento { get; }

    public int Cantidad { get; }

    internal ComponenteKit(
        IElementoCatalogo elemento,
        int cantidad)
    {
        ArgumentNullException.ThrowIfNull(elemento);

        if (cantidad <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cantidad),
                "La cantidad debe ser mayor que cero.");
        }

        Elemento = elemento;
        Cantidad = cantidad;
    }
}