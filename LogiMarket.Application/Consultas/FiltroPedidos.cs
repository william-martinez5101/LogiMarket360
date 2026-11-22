using System.Globalization;
using System.Text.RegularExpressions;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Application.Consultas;

public interface IExpresionPedido { bool Evaluar(Pedido pedido); }
public sealed record EstadoEs(EstadoPedido Estado) : IExpresionPedido { public bool Evaluar(Pedido p) => p.Estado == Estado; }
public sealed record TotalAlMenos(decimal Minimo) : IExpresionPedido { public bool Evaluar(Pedido p) => p.Valores.Total >= Minimo; }
public sealed record ClienteEs(string Nombre) : IExpresionPedido { public bool Evaluar(Pedido p) => string.Equals(p.ClienteNombre, Nombre, StringComparison.OrdinalIgnoreCase); }
public sealed record ExpresionAnd(IExpresionPedido Izquierda, IExpresionPedido Derecha) : IExpresionPedido { public bool Evaluar(Pedido p) => Izquierda.Evaluar(p) && Derecha.Evaluar(p); }
public sealed record ExpresionOr(IExpresionPedido Izquierda, IExpresionPedido Derecha) : IExpresionPedido { public bool Evaluar(Pedido p) => Izquierda.Evaluar(p) || Derecha.Evaluar(p); }
public sealed class FiltroPedidosParser
{
    private enum Tipo { Identificador, Texto, Numero, Operador, Parentesis, Logico }
    private sealed record Token(string Valor, int Posicion, Tipo Tipo);
    private static readonly Regex Lexico = new("\\G\\s*(>=|=|\\(|\\)|\"[^\"]*\"|'[^']*'|[\\p{L}_][\\w.]*|[0-9]+(?:[.,][0-9]+)?)", RegexOptions.CultureInvariant);
    private List<Token> _tokens = []; private int _indice, _fin;
    private static Tipo Clasificar(string valor) => valor switch
    {
        "=" or ">=" => Tipo.Operador,
        "(" or ")" => Tipo.Parentesis,
        _ when valor[0] is '\'' or '"' => Tipo.Texto,
        _ when char.IsDigit(valor[0]) => Tipo.Numero,
        _ when valor.Equals("AND", StringComparison.OrdinalIgnoreCase) || valor.Equals("OR", StringComparison.OrdinalIgnoreCase) => Tipo.Logico,
        _ => Tipo.Identificador
    };
    public IExpresionPedido Interpretar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) throw new FormatException("Escriba una expresion: ESTADO = PAGADO AND TOTAL >= 50.");
        if (texto.Length > 2000) throw new FormatException("Filtro demasiado largo (maximo 2000 caracteres).");
        _tokens = []; _indice = 0; _fin = texto.Length + 1; int posicion = 0;
        while (posicion < texto.Length)
        {
            if (string.IsNullOrWhiteSpace(texto[posicion..])) break;
            var match = Lexico.Match(texto, posicion);
            if (!match.Success) throw new FormatException($"Caracter no permitido en posicion {posicion + 1}.");
            var valor = match.Groups[1].Value;
            _tokens.Add(new(valor, match.Groups[1].Index + 1, Clasificar(valor))); posicion += match.Length;
        }
        var ast = Or(0);
        if (_indice != _tokens.Count) throw Error("Se esperaba AND, OR o fin de expresion"); return ast;
    }
    private IExpresionPedido Or(int nivel)
    { var e = And(nivel); while (Tomar("OR")) e = new ExpresionOr(e, And(nivel)); return e; }
    private IExpresionPedido And(int nivel)
    { var e = Primaria(nivel); while (Tomar("AND")) e = new ExpresionAnd(e, Primaria(nivel)); return e; }
    private IExpresionPedido Primaria(int nivel)
    {
        if (nivel > 32) throw Error("Demasiados parentesis anidados");
        if (Tomar("(")) { var e = Or(nivel + 1); Exigir(")"); return e; }
        var campo = Siguiente(Tipo.Identificador, "Se esperaba un campo").Valor.ToUpperInvariant();
        if (campo == "ESTADO")
        {
            Exigir("="); var valor = Siguiente(Tipo.Identificador, "ESTADO requiere un nombre de estado");
            if (!Enum.TryParse<EstadoPedido>(valor.Valor, true, out var estado) || !Enum.IsDefined(estado)) throw ErrorEn("Estado desconocido", valor.Posicion);
            return new EstadoEs(estado);
        }
        if (campo == "TOTAL")
        {
            Exigir(">="); var valor = Siguiente(Tipo.Numero, "TOTAL requiere un numero no negativo");
            if (!decimal.TryParse(valor.Valor.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var minimo) || minimo < 0) throw ErrorEn("TOTAL requiere un numero no negativo", valor.Posicion);
            return new TotalAlMenos(minimo);
        }
        if (campo == "CLIENTE")
        {
            Exigir("=");
            if (_indice >= _tokens.Count || _tokens[_indice].Tipo is not (Tipo.Identificador or Tipo.Texto)) throw Error("CLIENTE requiere un nombre o texto entre comillas");
            var valor = _tokens[_indice++]; var nombre = valor.Tipo == Tipo.Texto ? valor.Valor[1..^1] : valor.Valor;
            if (string.IsNullOrWhiteSpace(nombre)) throw ErrorEn("CLIENTE requiere un nombre", valor.Posicion);
            return new ClienteEs(nombre);
        }
        throw Error("Campo desconocido; use ESTADO, TOTAL o CLIENTE");
    }
    private bool Tomar(string valor)
    { if (_indice >= _tokens.Count || !string.Equals(_tokens[_indice].Valor, valor, StringComparison.OrdinalIgnoreCase)) return false; _indice++; return true; }
    private void Exigir(string valor) { if (!Tomar(valor)) throw Error($"Se esperaba '{valor}'"); }
    private Token Siguiente(Tipo tipo, string mensaje)
    { if (_indice >= _tokens.Count || _tokens[_indice].Tipo != tipo) throw Error(mensaje); return _tokens[_indice++]; }
    private FormatException Error(string mensaje) => ErrorEn(mensaje, _indice < _tokens.Count ? _tokens[_indice].Posicion : _fin);
    private static FormatException ErrorEn(string mensaje, int posicion) => new($"{mensaje}, posicion {posicion}.");
}
