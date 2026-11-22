namespace LogiMarket.Domain.Ventas;

public sealed class PedidoBorrador
{
    private List<LineaPedido> _lineas = new();
    private bool _bloqueado;
    public Guid? ClienteId { get; private set; }
    public TipoEntrega Entrega { get; private set; }
    public IReadOnlyList<LineaPedido> Lineas => _lineas.AsReadOnly();
    private void Editable() { if (_bloqueado) throw new InvalidOperationException("El borrador ya fue registrado; no se puede deshacer un pago."); }
    public void SeleccionarCliente(Guid? id) { Editable(); ClienteId = id; }
    public void SeleccionarEntrega(TipoEntrega entrega) { Editable(); Entrega = entrega; }
    public void Agregar(LineaPedido linea) { Editable(); _lineas.Add(linea.Copiar()); }
    public void Quitar(int indice) { Editable(); _lineas.RemoveAt(indice); }
    public void CambiarCantidad(int indice, int cantidad) { Editable(); _lineas[indice] = _lineas[indice].Copiar(cantidad); }
    public PedidoBorrador Clonar()
    {
        Editable(); var copia = new PedidoBorrador(); copia.Restaurar(Capturar()); return copia;
    }
    internal RecuerdoBorrador Capturar() => new(ClienteId, Entrega, _lineas.Select(x => x.Copiar()).ToList());
    internal void Restaurar(RecuerdoBorrador recuerdo)
    { Editable(); ClienteId = recuerdo.ClienteId; Entrega = recuerdo.Entrega; _lineas = recuerdo.Lineas.Select(x => x.Copiar()).ToList(); }
    public void Bloquear() => _bloqueado = true;
}
internal sealed record RecuerdoBorrador(Guid? ClienteId, TipoEntrega Entrega, IReadOnlyList<LineaPedido> Lineas);

public sealed class HistorialBorrador(PedidoBorrador borrador)
{
    private readonly List<RecuerdoBorrador> _estados = [borrador.Capturar()];
    private int _posicion;
    public bool PuedeDeshacer => _posicion > 0;
    public bool PuedeRehacer => _posicion + 1 < _estados.Count;
    public int CantidadEstados => _estados.Count;
    public void Editar(Action<PedidoBorrador> edicion)
    {
        var antes = borrador.Capturar();
        try { edicion(borrador); }
        catch { borrador.Restaurar(antes); throw; }
        _estados.RemoveRange(_posicion + 1, _estados.Count - _posicion - 1);
        _estados.Add(borrador.Capturar());
        if (_estados.Count > 5) _estados.RemoveAt(0);
        _posicion = _estados.Count - 1;
    }
    public void Deshacer()
    { if (!PuedeDeshacer) throw new InvalidOperationException("No hay ediciones anteriores."); borrador.Restaurar(_estados[_posicion - 1]); _posicion--; }
    public void Rehacer()
    { if (!PuedeRehacer) throw new InvalidOperationException("No hay ediciones posteriores."); borrador.Restaurar(_estados[_posicion + 1]); _posicion++; }
    public void Confirmado() { borrador.Bloquear(); _estados.Clear(); _posicion = 0; }
}
