using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;

namespace LogiMarket.Domain.Ventas;

public sealed record CambioEstado(EstadoPedido? Desde, EstadoPedido Hasta, DateTimeOffset Fecha, string Actor);

public sealed class Pedido
{
    private EstadoDelPedido _estado;
    private readonly List<CambioEstado> _historial;
    private List<LineaPedido> _lineas;
    public Guid Id { get; }
    public Guid ClienteId { get; private set; }
    public string ClienteNombre { get; private set; }
    public CategoriaCliente Categoria { get; private set; }
    public IReadOnlyList<LineaPedido> Lineas => _lineas.AsReadOnly();
    public PlanEntrega Entrega { get; private set; }
    public Valoracion Valores { get; private set; }
    public EstadoPedido Estado => _estado.Codigo;
    public IReadOnlyList<CambioEstado> Historial => _historial.AsReadOnly();
    public DateTimeOffset Creado { get; }
    public DateTimeOffset? FechaEntrega { get; private set; }
    public bool Devuelto { get; private set; }
    public long Version { get; }

    internal Pedido(Guid id, Guid clienteId, string nombre, CategoriaCliente categoria, IEnumerable<LineaPedido> lineas,
        PlanEntrega entrega, Valoracion valores, DateTimeOffset creado, string actor)
        : this(id, clienteId, nombre, categoria, lineas, entrega, valores, creado, EstadoPedido.BORRADOR,
            [new(null, EstadoPedido.BORRADOR, creado, actor)], null, false, 0) { }

    private Pedido(Guid id, Guid clienteId, string nombre, CategoriaCliente categoria, IEnumerable<LineaPedido> lineas,
        PlanEntrega entrega, Valoracion valores, DateTimeOffset creado, EstadoPedido estado,
        IEnumerable<CambioEstado> historial, DateTimeOffset? fechaEntrega, bool devuelto, long version)
    {
        if (id == Guid.Empty || clienteId == Guid.Empty || string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El pedido necesita identidad y cliente.");
        _lineas = lineas.Select(l => l.Copiar()).ToList();
        if (_lineas.Count == 0) throw new InvalidOperationException("El pedido necesita lineas.");
        if (valores.Subtotal != _lineas.Sum(l => l.Importe) || valores.Total != valores.Subtotal - valores.Descuento + valores.Tarifa)
            throw new InvalidOperationException("Valoracion inconsistente.");
        Id = id; ClienteId = clienteId; ClienteNombre = nombre; Categoria = categoria;
        Entrega = entrega; Valores = valores; Creado = creado; _estado = EstadoDelPedido.Desde(estado);
        _historial = historial.ToList(); FechaEntrega = fechaEntrega; Devuelto = devuelto; Version = version;
    }
    public static Pedido Rehidratar(Guid id, Guid clienteId, string nombre, CategoriaCliente categoria, IEnumerable<LineaPedido> lineas,
        PlanEntrega entrega, Valoracion valores, DateTimeOffset creado, EstadoPedido estado,
        IEnumerable<CambioEstado> historial, DateTimeOffset? fechaEntrega, bool devuelto, long version)
        => new(id, clienteId, nombre, categoria, lineas, entrega, valores, creado, estado, historial, fechaEntrega, devuelto, version);

    public IReadOnlyList<ComponenteFisico> Fisicos() => _lineas.SelectMany(l => l.Fisicos()).GroupBy(x => x.Sku)
        .Select(g => new ComponenteFisico(g.Key, g.First().Nombre,
            g.Aggregate(0, (n, x) => checked(n + x.Cantidad)), g.First().Precio, g.First().PesoKg)).OrderBy(x => x.Sku).ToList();
    public void Validar(DateTimeOffset fecha, string actor) => Transicion(_estado.Validar(), fecha, actor);
    public void Pagar(DateTimeOffset fecha, string actor) => Transicion(_estado.Pagar(), fecha, actor);
    public void Despachar(DateTimeOffset fecha, string actor) => Transicion(_estado.Despachar(), fecha, actor);
    public void Entregar(DateTimeOffset fecha, string actor)
    { var destino = _estado.Entregar(); Transicion(destino, fecha, actor); FechaEntrega = fecha; }
    public void Cancelar(DateTimeOffset fecha, string actor) => Transicion(_estado.Cancelar(), fecha, actor);
    private void Transicion(EstadoDelPedido destino, DateTimeOffset fecha, string actor)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor obligatorio.");
        if (fecha < _historial.Last().Fecha) throw new InvalidOperationException("La fecha no puede retroceder.");
        _historial.Add(new(Estado, destino.Codigo, fecha, actor)); _estado = destino;
    }
    public void EditarDesde(Pedido nuevo)
    {
        _estado.PermitirEdicion();
        if (nuevo.Estado != EstadoPedido.BORRADOR) throw new InvalidOperationException("Se requiere un borrador.");
        ClienteId = nuevo.ClienteId; ClienteNombre = nuevo.ClienteNombre; Categoria = nuevo.Categoria;
        _lineas = nuevo.Lineas.Select(x => x.Copiar()).ToList();
        Entrega = nuevo.Entrega with { Guia = $"GUIA-{nuevo.Entrega.Tipo}-{Id:N}" }; Valores = nuevo.Valores;
    }
    public Pedido ClonarBorrador(DateTimeOffset fecha, string actor)
    {
        _estado.PermitirEdicion();
        var id = Guid.NewGuid();
        return new(id, ClienteId, ClienteNombre, Categoria, _lineas, Entrega with { Guia = $"GUIA-{Entrega.Tipo}-{id:N}" }, Valores, fecha, actor);
    }
    public void RegistrarDevolucion()
    {
        if (Estado != EstadoPedido.ENTREGADO || Devuelto) throw new InvalidOperationException("El pedido no admite otra devolucion.");
        Devuelto = true;
    }
}

public sealed class PedidoBuilder(CalculadoraPedido calculadora, IEnumerable<FabricaEntrega> entregas)
{
    private Cliente? _cliente;
    private readonly List<LineaPedido> _lineas = new();
    private TipoEntrega _entrega;
    public PedidoBuilder ParaCliente(Cliente cliente) { _cliente = cliente; return this; }
    public PedidoBuilder Agregar(IElementoCatalogo elemento, int cantidad)
    { _lineas.Add(new(NodoInstantanea.Capturar(elemento), cantidad)); return this; }
    public PedidoBuilder AgregarInstantanea(LineaPedido linea) { _lineas.Add(linea.Copiar()); return this; }
    public PedidoBuilder ConEntrega(TipoEntrega entrega) { _entrega = entrega; return this; }
    public Pedido Construir(DateTimeOffset fecha, string actor)
    {
        if (_cliente is null || !_cliente.Activo) throw new InvalidOperationException("Seleccione un cliente activo.");
        if (_lineas.Count == 0) throw new InvalidOperationException("Agregue al menos una linea.");
        var id = Guid.NewGuid();
        var plan = entregas.Single(f => f.Tipo == _entrega).CrearPlan(id);
        return new(id, _cliente.Id, _cliente.Nombre, _cliente.Categoria, _lineas, plan,
            calculadora.Calcular(_cliente.Categoria, _lineas, plan.Tarifa), fecha, actor);
    }
}
