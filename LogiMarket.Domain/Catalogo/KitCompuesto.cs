using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiMarket.Domain.Catalogo;

public sealed class KitCompuesto : IElementoCatalogo
{
    public void Aceptar(IVisitanteCatalogo visitante, int cantidad = 1)
        => visitante.Visitar(this, Cantidades.Validar(cantidad));

    private readonly List<ComponenteKit> _componentes = new();

    public Guid Id { get; private set; }

    public string Codigo { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public IReadOnlyList<ComponenteKit> Componentes
        => _componentes.AsReadOnly();

    // Constructor reservado para la persistencia.
    private KitCompuesto()
    {
    }

    public KitCompuesto(string codigo, string nombre)
    {
        var codigoValidado = TextoObligatorio(
            codigo,
            nameof(codigo));

        var nombreValidado = TextoObligatorio(
            nombre,
            nameof(nombre));

        Id = Guid.NewGuid();
        Codigo = codigoValidado.ToUpperInvariant();
        Nombre = nombreValidado;
    }

    public void Agregar(
        IElementoCatalogo elemento,
        int cantidad = 1)
    {
        // El componente valida la referencia y la cantidad.
        var componente = new ComponenteKit(elemento, cantidad);

        // Agregar un kit que ya contiene al actual formaria un ciclo.
        if (elemento is KitCompuesto kit &&
            kit.ContieneKit(Id, new HashSet<Guid>()))
        {
            throw new InvalidOperationException(
                "El componente generaria una referencia circular.");
        }

        _componentes.Add(componente);
    }

    // Composite: cada hijo calcula su propio precio.
    public decimal CalcularPrecio()
    {
        if (_componentes.Count == 0)
        {
            throw new InvalidOperationException(
                "El kit debe contener componentes para calcular su precio.");
        }

        return _componentes.Sum(componente =>
            componente.Elemento.CalcularPrecio() *
            componente.Cantidad);
    }

    private bool ContieneKit(
        Guid idBuscado,
        HashSet<Guid> visitados)
    {
        if (Id == idBuscado)
        {
            return true;
        }

        // Un kit compartido puede aparecer en distintas ramas.
        if (!visitados.Add(Id))
        {
            return false;
        }

        foreach (var componente in _componentes)
        {
            if (componente.Elemento is KitCompuesto kit &&
                kit.ContieneKit(idBuscado, visitados))
            {
                return true;
            }
        }

        return false;
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
}
