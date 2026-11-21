using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LogiMarket.Domain.Clientes;
using Xunit;

namespace LogiMarket.Tests.Domain.Clientes;

public sealed class ClienteTests
{
    [Fact]
    public void CrearCliente_ConDatosValidos_IniciaActivo()
    {
        var cliente = new Cliente(
            "  Ana  ",
            CategoriaCliente.Vip);

        Assert.NotEqual(Guid.Empty, cliente.Id);
        Assert.Equal("Ana", cliente.Nombre);
        Assert.Equal(CategoriaCliente.Vip, cliente.Categoria);
        Assert.True(cliente.Activo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CrearCliente_ConNombreInvalido_RechazaOperacion(
        string? nombre)
    {
        // null! permite probar deliberadamente una entrada invalida.
        Assert.Throws<ArgumentException>(() =>
            new Cliente(nombre!, CategoriaCliente.Normal));
    }

    [Fact]
    public void CrearCliente_ConCategoriaInvalida_RechazaOperacion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Cliente("Ana", (CategoriaCliente)999));
    }

    [Fact]
    public void ActualizarDatos_PreservaIdentidadYEstadoDeActividad()
    {
        var cliente = new Cliente(
            "Ana",
            CategoriaCliente.Normal);

        var idOriginal = cliente.Id;
        cliente.Desactivar();

        cliente.ActualizarDatos(
            "  Ana Martinez  ",
            CategoriaCliente.Vip);

        Assert.Equal(idOriginal, cliente.Id);
        Assert.Equal("Ana Martinez", cliente.Nombre);
        Assert.Equal(CategoriaCliente.Vip, cliente.Categoria);
        Assert.False(cliente.Activo);
    }

    [Fact]
    public void ActualizarDatos_ConCategoriaInvalida_NoModificaLosDatos()
    {
        var cliente = new Cliente(
            "Ana",
            CategoriaCliente.Normal);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            cliente.ActualizarDatos(
                "Nombre nuevo",
                (CategoriaCliente)999));

        Assert.Equal("Ana", cliente.Nombre);
        Assert.Equal(CategoriaCliente.Normal, cliente.Categoria);
        Assert.True(cliente.Activo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ActualizarDatos_ConNombreInvalido_NoModificaLosDatos(
        string? nombre)
    {
        var cliente = new Cliente(
            "Ana",
            CategoriaCliente.Normal);

        Assert.Throws<ArgumentException>(() =>
            cliente.ActualizarDatos(
                nombre!,
                CategoriaCliente.Vip));

        Assert.Equal("Ana", cliente.Nombre);
        Assert.Equal(CategoriaCliente.Normal, cliente.Categoria);
    }

    [Fact]
    public void Activar_DespuesDeDesactivar_RestauraEstadoActivo()
    {
        var cliente = new Cliente(
            "Luis",
            CategoriaCliente.Normal);

        cliente.Desactivar();
        Assert.False(cliente.Activo);

        cliente.Activar();
        Assert.True(cliente.Activo);
    }
}