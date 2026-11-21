using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiMarket.Domain.Clientes;

public sealed class Cliente
{
    public Guid Id { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public CategoriaCliente Categoria { get; private set; }

    public bool Activo { get; private set; }

    // Constructor reservado para la persistencia.
    private Cliente()
    {
    }

    public Cliente(string nombre, CategoriaCliente categoria)
    {
        var nombreValidado = ValidarNombre(nombre);
        ValidarCategoria(categoria);

        Id = Guid.NewGuid();
        Nombre = nombreValidado;
        Categoria = categoria;
        Activo = true;
    }

    public void ActualizarDatos(
        string nombre,
        CategoriaCliente categoria)
    {
        // Validar todo antes de modificar evita cambios parciales.
        var nombreValidado = ValidarNombre(nombre);
        ValidarCategoria(categoria);

        Nombre = nombreValidado;
        Categoria = categoria;
    }

    public void Activar()
    {
        Activo = true;
    }

    public void Desactivar()
    {
        Activo = false;
    }

    private static string ValidarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException(
                "El nombre del cliente es obligatorio.",
                nameof(nombre));
        }

        return nombre.Trim();
    }

    private static void ValidarCategoria(
        CategoriaCliente categoria)
    {
        if (!Enum.IsDefined(typeof(CategoriaCliente), categoria))
        {
            throw new ArgumentOutOfRangeException(
                nameof(categoria),
                categoria,
                "La categoria del cliente no es valida.");
        }
    }
}
