using LogiMarket.Application.Consultas;

namespace LogiMarket.Tests.Integracion;

public sealed class RegresionesFiltrosRevisionTests
{
    [Theory]
    [InlineData("CLIENTE = =", 11)]
    [InlineData("CLIENTE = >=", 11)]
    [InlineData("CLIENTE = and", 11)]
    [InlineData("CLIENTE = or", 11)]
    [InlineData("CLIENTE = 123", 11)]
    [InlineData("CLIENTE = ''", 11)]
    [InlineData("CLIENTE = '  '", 11)]
    [InlineData("  CLIENTE  =  >=", 15)]
    [InlineData("CLIENTE =", 10)]
    [InlineData("ESTADO = 2", 10)]
    [InlineData("TOTAL >= '50'", 10)]
    public void ValoresInvalidos_InformanLaPosicionDelToken(string texto, int posicion)
    {
        var error = Assert.Throws<FormatException>(() => new FiltroPedidosParser().Interpretar(texto));
        Assert.Contains("posicion " + posicion + ".", error.Message);
    }

    [Theory]
    [InlineData("CLIENTE = 'Ana Maria'", "Ana Maria")]
    [InlineData("CLIENTE = \"Ángela Núñez\"", "Ángela Núñez")]
    [InlineData("CLIENTE = Ángela", "Ángela")]
    [InlineData("CLIENTE = 'AND'", "AND")]
    [InlineData("CLIENTE = '='", "=")]
    [InlineData("cliente = Ana", "Ana")]
    public void NombresValidos_ConservanSuTexto(string texto, string esperado)
        => Assert.Equal(esperado, Assert.IsType<ClienteEs>(new FiltroPedidosParser().Interpretar(texto)).Nombre);

    [Fact]
    public void PrecedenciaYParentesis_ConservanElAst()
    {
        var p = new FiltroPedidosParser();
        var ast = Assert.IsType<ExpresionOr>(p.Interpretar("CLIENTE = 'Ana' OR ESTADO = PAGADO AND TOTAL >= 50"));
        Assert.IsType<ClienteEs>(ast.Izquierda); Assert.IsType<ExpresionAnd>(ast.Derecha);
        var grupo = Assert.IsType<ExpresionAnd>(p.Interpretar("(CLIENTE = 'Ana' OR ESTADO = PAGADO) AND TOTAL >= 50"));
        Assert.IsType<ExpresionOr>(grupo.Izquierda); Assert.IsType<TotalAlMenos>(grupo.Derecha);
    }
}
