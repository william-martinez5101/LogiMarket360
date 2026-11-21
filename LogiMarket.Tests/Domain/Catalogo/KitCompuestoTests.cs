using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiMarket.Domain.Catalogo;
using Xunit;

namespace LogiMarket.Tests.Domain.Catalogo;

public sealed class KitCompuestoTests
{
    [Fact]
    public void KitOficina_CalcularPrecio_Devuelve50()
    {
        IElementoCatalogo oficina = CrearOficina();

        Assert.Equal(50m, oficina.CalcularPrecio());
    }

    [Fact]
    public void KitTeletrabajo_CalcularPrecio_Devuelve60()
    {
        IElementoCatalogo teletrabajo = CrearTeletrabajo();

        Assert.Equal(60m, teletrabajo.CalcularPrecio());
    }

    [Fact]
    public void CalcularPrecio_ConCantidadesAnidadas_PropagaMultiplicadores()
    {
        var paquete = new KitCompuesto(
            "PAQUETE",
            "Dos oficinas y tres memorias");

        paquete.Agregar(CrearOficina(), 2);

        paquete.Agregar(
            new ProductoSimple(
                "P03",
                "Memoria USB",
                10m,
                0.10m,
                20),
            3);

        // Dos kits de 50 y tres memorias de 10.
        Assert.Equal(130m, paquete.CalcularPrecio());
    }

    [Fact]
    public void Agregar_ElMismoKit_RechazaSinModificarComponentes()
    {
        var oficina = CrearOficina();

        Assert.Throws<InvalidOperationException>(() =>
            oficina.Agregar(oficina));

        Assert.Equal(2, oficina.Componentes.Count);
        Assert.Equal(50m, oficina.CalcularPrecio());
    }

    [Fact]
    public void Agregar_ConCicloIndirecto_RechazaOperacion()
    {
        var teletrabajo = CrearTeletrabajo();

        var oficina = Assert.IsType<KitCompuesto>(
            teletrabajo.Componentes[0].Elemento);

        // Teletrabajo ya contiene Oficina.
        // Oficina no puede contener ahora a Teletrabajo.
        Assert.Throws<InvalidOperationException>(() =>
            oficina.Agregar(teletrabajo));

        Assert.Equal(50m, oficina.CalcularPrecio());
        Assert.Equal(60m, teletrabajo.CalcularPrecio());
    }

    [Fact]
    public void Agregar_ConCicloDeTresNiveles_RechazaOperacion()
    {
        var primero = CrearOficina();
        var segundo = new KitCompuesto("K2", "Segundo");
        var tercero = new KitCompuesto("K3", "Tercero");

        segundo.Agregar(primero);
        tercero.Agregar(segundo);

        Assert.Throws<InvalidOperationException>(() =>
            primero.Agregar(tercero));

        Assert.Equal(2, primero.Componentes.Count);
        Assert.Equal(50m, tercero.CalcularPrecio());
    }

    [Fact]
    public void ReutilizarKit_EnDosRamas_NoProduceFalsoCiclo()
    {
        var compartido = CrearOficina();

        var izquierda = new KitCompuesto("IZQ", "Izquierda");
        var derecha = new KitCompuesto("DER", "Derecha");
        var conjunto = new KitCompuesto("CONJUNTO", "Conjunto");

        izquierda.Agregar(compartido);
        derecha.Agregar(compartido);

        conjunto.Agregar(izquierda);
        conjunto.Agregar(derecha);

        Assert.Equal(100m, conjunto.CalcularPrecio());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Agregar_ConCantidadInvalida_ConservaElKit(
        int cantidad)
    {
        var oficina = CrearOficina();

        var memoria = new ProductoSimple(
            "P03",
            "Memoria USB",
            10m,
            0.10m,
            20);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            oficina.Agregar(memoria, cantidad));

        Assert.Equal(2, oficina.Componentes.Count);
        Assert.Equal(50m, oficina.CalcularPrecio());
    }

    [Fact]
    public void Agregar_ConElementoNulo_RechazaOperacion()
    {
        var oficina = CrearOficina();

        Assert.Throws<ArgumentNullException>(() =>
            oficina.Agregar(null!));

        Assert.Equal(2, oficina.Componentes.Count);
    }

    [Fact]
    public void CalcularPrecio_ConKitVacio_RechazaOperacion()
    {
        var kit = new KitCompuesto("VACIO", "Kit sin componentes");

        Assert.Throws<InvalidOperationException>(() =>
            kit.CalcularPrecio());
    }

    [Fact]
    public void Componentes_NoPermiteModificarLaColeccionDesdeFuera()
    {
        var oficina = CrearOficina();

        var coleccion = Assert.IsAssignableFrom<IList<ComponenteKit>>(
            oficina.Componentes);

        Assert.Throws<NotSupportedException>(() =>
            coleccion.Clear());

        Assert.Equal(2, oficina.Componentes.Count);
        Assert.Equal(50m, oficina.CalcularPrecio());
    }

    [Theory]
    [InlineData(null, "Oficina")]
    [InlineData("", "Oficina")]
    [InlineData("   ", "Oficina")]
    [InlineData("KIT_OFICINA", null)]
    [InlineData("KIT_OFICINA", "")]
    [InlineData("KIT_OFICINA", "   ")]
    public void CrearKit_ConTextoInvalido_RechazaOperacion(
        string? codigo,
        string? nombre)
    {
        Assert.Throws<ArgumentException>(() =>
            new KitCompuesto(codigo!, nombre!));
    }

    [Fact]
    public void CalcularPrecio_ReflejaCambiosDelCatalogoActual()
    {
        var oficina = CrearOficina();

        var mouse = Assert.IsType<ProductoSimple>(
            oficina.Componentes[0].Elemento);

        mouse.ActualizarDatos(
            "Mouse USB",
            25m,
            0.20m);

        Assert.Equal(55m, oficina.CalcularPrecio());
    }

    private static KitCompuesto CrearOficina()
    {
        var mouse = new ProductoSimple(
            "P01",
            "Mouse USB",
            20m,
            0.20m,
            8);

        var teclado = new ProductoSimple(
            "P02",
            "Teclado",
            30m,
            0.70m,
            5);

        var oficina = new KitCompuesto(
            "KIT_OFICINA",
            "Kit oficina");

        oficina.Agregar(mouse);
        oficina.Agregar(teclado);

        return oficina;
    }

    private static KitCompuesto CrearTeletrabajo()
    {
        var teletrabajo = new KitCompuesto(
            "KIT_TELETRABAJO",
            "Kit teletrabajo");

        teletrabajo.Agregar(CrearOficina());

        teletrabajo.Agregar(
            new ProductoSimple(
                "P03",
                "Memoria USB",
                10m,
                0.10m,
                20));

        return teletrabajo;
    }
}