using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiMarket.Domain.Catalogo;

public interface IElementoCatalogo
{
    Guid Id { get; }

    string Codigo { get; }

    string Nombre { get; }

    decimal CalcularPrecio();
}