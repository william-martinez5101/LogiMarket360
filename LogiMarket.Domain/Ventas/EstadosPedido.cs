namespace LogiMarket.Domain.Ventas;

public enum EstadoPedido { BORRADOR, VALIDADO, PAGADO, DESPACHADO, ENTREGADO, CANCELADO }

public abstract class EstadoDelPedido
{
    public abstract EstadoPedido Codigo { get; }
    public virtual EstadoDelPedido Validar() => Denegar();
    public virtual EstadoDelPedido Pagar() => Denegar();
    public virtual EstadoDelPedido Despachar() => Denegar();
    public virtual EstadoDelPedido Entregar() => Denegar();
    public virtual EstadoDelPedido Cancelar() => Denegar();
    public virtual void PermitirEdicion() => throw new InvalidOperationException($"No se puede editar un pedido {Codigo}.");
    private EstadoDelPedido Denegar() => throw new InvalidOperationException($"Operacion no permitida desde {Codigo}.");
    private static readonly IReadOnlyDictionary<EstadoPedido, EstadoDelPedido> Estados = new Dictionary<EstadoPedido, EstadoDelPedido>
    {
        [EstadoPedido.BORRADOR] = new EstadoBorrador(), [EstadoPedido.VALIDADO] = new EstadoValidado(),
        [EstadoPedido.PAGADO] = new EstadoPagado(), [EstadoPedido.DESPACHADO] = new EstadoDespachado(),
        [EstadoPedido.ENTREGADO] = new EstadoEntregado(), [EstadoPedido.CANCELADO] = new EstadoCancelado()
    };
    public static EstadoDelPedido Desde(EstadoPedido codigo) => Estados.TryGetValue(codigo, out var estado) ? estado
        : throw new ArgumentException("Estado desconocido.");
}
public sealed class EstadoBorrador : EstadoDelPedido
{
    public override EstadoPedido Codigo => EstadoPedido.BORRADOR;
    public override EstadoDelPedido Validar() => new EstadoValidado();
    public override EstadoDelPedido Cancelar() => new EstadoCancelado();
    public override void PermitirEdicion() { }
}
public sealed class EstadoValidado : EstadoDelPedido
{
    public override EstadoPedido Codigo => EstadoPedido.VALIDADO;
    public override EstadoDelPedido Pagar() => new EstadoPagado();
    public override EstadoDelPedido Cancelar() => new EstadoCancelado();
}
public sealed class EstadoPagado : EstadoDelPedido
{
    public override EstadoPedido Codigo => EstadoPedido.PAGADO;
    public override EstadoDelPedido Despachar() => new EstadoDespachado();
}
public sealed class EstadoDespachado : EstadoDelPedido
{
    public override EstadoPedido Codigo => EstadoPedido.DESPACHADO;
    public override EstadoDelPedido Entregar() => new EstadoEntregado();
}
public sealed class EstadoEntregado : EstadoDelPedido { public override EstadoPedido Codigo => EstadoPedido.ENTREGADO; }
public sealed class EstadoCancelado : EstadoDelPedido { public override EstadoPedido Codigo => EstadoPedido.CANCELADO; }
