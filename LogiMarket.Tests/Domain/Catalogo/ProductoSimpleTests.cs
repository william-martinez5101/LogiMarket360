using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiMarket.Domain.Catalogo;
using Xunit;

namespace LogiMarket.Tests.Domain.Catalogo;

public sealed class ProductoSimpleTests
{
    [Fact]
    public void CrearProducto_ConDatosValidos_NormalizaCodigoYNombre()
    {
        var producto = new ProductoSimple(
            " p01 ",
            " Mouse USB ",
            20m,
            0.20m,
            8);

        Assert.NotEqual(Guid.Empty, producto.Id);
        Assert.Equal("P01", producto.Sku);
        Assert.Equal("Mouse USB", producto.Nombre);
        Assert.Equal(20m, producto.Precio);
        Assert.Equal(0.20m, producto.PesoKg);
        Assert.Equal(8, producto.Stock);
    }

    [Theory]
    [InlineData(null, "Mouse")]
    [InlineData("", "Mouse")]
    [InlineData("   ", "Mouse")]
    [InlineData("P01", null)]
    [InlineData("P01", "")]
    [InlineData("P01", "   ")]
    public void CrearProducto_ConTextoInvalido_RechazaOperacion(
        string? sku,
        string? nombre)
    {
        Assert.Throws<ArgumentException>(() =>
            new ProductoSimple(
                sku!,
                nombre!,
                20m,
                0.20m,
                8));
    }

    [Theory]
    [InlineData(0, 1, 8)]
    [InlineData(-1, 1, 8)]
    [InlineData(20, -1, 8)]
    [InlineData(20, 1, -1)]
    public void CrearProducto_ConValoresFueraDeRango_RechazaOperacion(
        int precio,
        int pesoKg,
        int stock)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProductoSimple(
                "P01",
                "Mouse USB",
                precio,
                pesoKg,
                stock));
    }

    [Fact]
    public void CrearProducto_ConPesoYStockCero_PermiteOperacion()
    {
        var producto = new ProductoSimple(
            "P01",
            "Mouse USB",
            20m,
            0m,
            0);

        Assert.Equal(0m, producto.PesoKg);
        Assert.Equal(0, producto.Stock);
    }

    [Fact]
    public void ActualizarDatos_ConservaIdentidadSkuYStock()
    {
        var producto = CrearMouse();
        var idOriginal = producto.Id;

        producto.ActualizarDatos(
            " Mouse actualizado ",
            25m,
            0.25m);

        Assert.Equal(idOriginal, producto.Id);
        Assert.Equal("P01", producto.Sku);
        Assert.Equal(8, producto.Stock);
        Assert.Equal("Mouse actualizado", producto.Nombre);
        Assert.Equal(25m, producto.Precio);
        Assert.Equal(0.25m, producto.PesoKg);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(25, -1)]
    public void ActualizarDatos_ConValoresInvalidos_NoModificaLosDatos(
        int precio,
        int pesoKg)
    {
        var producto = CrearMouse();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            producto.ActualizarDatos(
                "Nombre nuevo",
                precio,
                pesoKg));

        Assert.Equal("Mouse USB", producto.Nombre);
        Assert.Equal(20m, producto.Precio);
        Assert.Equal(0.20m, producto.PesoKg);
        Assert.Equal(8, producto.Stock);
    }

    [Theory]
    [InlineData(3, 5)]
    [InlineData(8, 0)]
    public void DescontarStock_ConCantidadDisponible_ActualizaExistencias(
        int cantidad,
        int stockEsperado)
    {
        var producto = CrearMouse();

        producto.DescontarStock(cantidad);

        Assert.Equal(stockEsperado, producto.Stock);
    }

    [Fact]
    public void DescontarStock_ConFaltante_RechazaSinModificarExistencias()
    {
        var producto = CrearMouse();

        Assert.Throws<InvalidOperationException>(() =>
            producto.DescontarStock(9));

        Assert.Equal(8, producto.Stock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CambiarStock_ConCantidadNoPositiva_RechazaOperacion(
        int cantidad)
    {
        var producto = CrearMouse();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            producto.DescontarStock(cantidad));

        Assert.Equal(8, producto.Stock);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            producto.ReintegrarStock(cantidad));

        Assert.Equal(8, producto.Stock);
    }

    [Fact]
    public void ReintegrarStock_ConCantidadValida_RecuperaExistencias()
    {
        var producto = CrearMouse();

        producto.DescontarStock(3);
        producto.ReintegrarStock(3);

        Assert.Equal(8, producto.Stock);
    }

    [Fact]
    public void ReintegrarStock_ConDesbordamiento_ConservaExistencias()
    {
        var producto = new ProductoSimple(
            "P01",
            "Mouse USB",
            20m,
            0.20m,
            int.MaxValue);

        Assert.Throws<OverflowException>(() =>
            producto.ReintegrarStock(1));

        Assert.Equal(int.MaxValue, producto.Stock);
    }

    private static ProductoSimple CrearMouse()
    {
        return new ProductoSimple(
            "P01",
            "Mouse USB",
            20m,
            0.20m,
            8);
    }
}