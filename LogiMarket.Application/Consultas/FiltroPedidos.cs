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
    private sealed record Token(string Valor, int Posicion);
    private static readonly Regex Lexico = new("\\G\\s*(>=|=|\\(|\\)|\"[^\"]*\"|'[^']*'|[A-Za-z_][\\w.]*|[0-9]+(?:[.,][0-9]+)?)", RegexOptions.CultureInvariant);
    private List<Token> _tokens = []; private int _indice;
    public IExpresionPedido Interpretar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) throw new FormatException("Escriba una expresion: ESTADO = PAGADO AND TOTAL >= 50.");
        if (texto.Length > 2000) throw new FormatException("Filtro demasiado largo (maximo 2000 caracteres).");
        _tokens = []; _indice = 0; int posicion = 0;
        while (posicion < texto.Length)
        {
            if (string.IsNullOrWhiteSpace(texto[posicion..])) break;
            var match = Lexico.Match(texto, posicion);
            if (!match.Success) throw new FormatException($"Caracter no permitido en posicion {posicion + 1}.");
            _tokens.Add(new(match.Groups[1].Value, posicion + 1)); posicion += match.Length;
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
        var campo = Siguiente().ToUpperInvariant();
        if (campo == "ESTADO")
        {
            Exigir("="); var valor = Siguiente();
            if (!Enum.TryParse<EstadoPedido>(valor, true, out var estado) || !Enum.IsDefined(estado)) throw Error("Estado desconocido");
            return new EstadoEs(estado);
        }
        if (campo == "TOTAL")
        {
            Exigir(">="); var valor = Siguiente().Replace(',', '.');
            if (!decimal.TryParse(valor, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var minimo) || minimo < 0) throw Error("TOTAL requiere un numero no negativo");
            return new TotalAlMenos(minimo);
        }
        if (campo == "CLIENTE")
        {
            Exigir("="); var nombre = Siguiente();
            if ((nombre.StartsWith('"') && nombre.EndsWith('"')) || (nombre.StartsWith('\'') && nombre.EndsWith('\''))) nombre = nombre[1..^1];
            if (string.IsNullOrWhiteSpace(nombre) || nombre is "(" or ")" or "AND" or "OR") throw Error("CLIENTE requiere un nombre");
            return new ClienteEs(nombre);
        }
        throw Error("Campo desconocido; use ESTADO, TOTAL o CLIENTE");
    }
    private bool Tomar(string valor)
    { if (_indice >= _tokens.Count || !string.Equals(_tokens[_indice].Valor, valor, StringComparison.OrdinalIgnoreCase)) return false; _indice++; return true; }
    private void Exigir(string valor) { if (!Tomar(valor)) throw Error($"Se esperaba '{valor}'"); }
    private string Siguiente() { if (_indice >= _tokens.Count) throw Error("Expresion incompleta"); return _tokens[_indice++].Valor; }
    private FormatException Error(string mensaje) => new($"{mensaje}, posicion {(_indice < _tokens.Count ? _tokens[_indice].Posicion : (_tokens.LastOrDefault()?.Posicion ?? 0) + 1)}.");
}
