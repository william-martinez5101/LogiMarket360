using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Ventas;
namespace LogiMarket.Application.Pedidos;
public sealed class ValoradorBorrador(CalculadoraPedido calculadora, IEnumerable<FabricaEntrega> entregas)
{
    public Valoracion? Valorar(PedidoBorrador borrador, Cliente? cliente)
        => cliente is null || borrador.Lineas.Count == 0 ? null
            : calculadora.Calcular(cliente.Categoria, borrador.Lineas, entregas.Single(x => x.Tipo == borrador.Entrega).CrearPlan(Guid.Empty).Tarifa);
}
