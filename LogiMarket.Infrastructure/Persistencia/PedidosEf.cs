using System.Text.Json;
using LogiMarket.Application.Contratos;
using LogiMarket.Domain.Ventas;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia;

internal sealed class PedidosEf(LogiMarketDbContext db) : IPedidoRepository
{
    private IQueryable<PedidoFila> Consulta => db.Set<PedidoFila>().Include(x => x.Detalles).ThenInclude(x => x.Fisicos).Include(x => x.Historial).AsSplitQuery();
    public async Task<Pedido?> BuscarAsync(Guid id)
    { var fila = await Consulta.SingleOrDefaultAsync(x => x.Id == id); return fila is null ? null : Dominio(fila); }
    public async Task<IReadOnlyList<Pedido>> ListarAsync() => (await Consulta.ToListAsync()).Select(Dominio).OrderByDescending(x => x.Creado).ToList();
    private static Pedido Dominio(PedidoFila p) => Pedido.Rehidratar(p.Id, p.ClienteId, p.ClienteNombre, p.Categoria,
        p.Detalles.OrderBy(x => x.Orden).Select(x => new LineaPedido(JsonSerializer.Deserialize<NodoInstantanea>(x.ArbolJson)!, x.Cantidad)),
        new(p.TipoEntrega, p.Tarifa, p.Dias, p.Guia), new(p.Subtotal, p.Descuento, p.Tarifa, p.Total, p.Promocion, p.Unidades),
        p.Creado, p.Estado, p.Historial.OrderBy(x => x.Orden).Select(x => new CambioEstado(x.Desde, x.Hasta, x.Fecha, x.Actor)), p.Entregado, p.Devuelto, p.Version);
    public async Task GuardarAsync(Pedido pedido)
    {
        var p = await Consulta.SingleOrDefaultAsync(x => x.Id == pedido.Id);
        var nuevo = p is null;
        if (nuevo) { p = new() { Id = pedido.Id }; db.Add(p); }
        if (!nuevo && p!.Version != pedido.Version) throw new DbUpdateConcurrencyException("El pedido fue modificado por otra sesion. Recargue.");
        p!.ClienteId = pedido.ClienteId; p.ClienteNombre = pedido.ClienteNombre; p.Categoria = pedido.Categoria;
        p.Estado = pedido.Estado; p.TipoEntrega = pedido.Entrega.Tipo; p.Dias = pedido.Entrega.Dias; p.Guia = pedido.Entrega.Guia;
        p.Subtotal = pedido.Valores.Subtotal; p.Descuento = pedido.Valores.Descuento; p.Tarifa = pedido.Valores.Tarifa; p.Total = pedido.Valores.Total;
        p.Promocion = pedido.Valores.Promocion; p.Unidades = pedido.Valores.Unidades; p.Creado = pedido.Creado; p.Entregado = pedido.FechaEntrega;
        p.Devuelto = pedido.Devuelto; p.Version = checked(pedido.Version + 1);
        foreach (var sobrante in p.Detalles.Where(d => d.Orden >= pedido.Lineas.Count).ToArray())
        { db.Remove(sobrante); p.Detalles.Remove(sobrante); }
        for (int i = 0; i < pedido.Lineas.Count; i++)
        {
            var linea = pedido.Lineas[i]; var arbol = JsonSerializer.Serialize(linea.Elemento);
            var d = p.Detalles.SingleOrDefault(x => x.Orden == i);
            if (d is null) { d = new() { Id = Guid.NewGuid(), PedidoId = pedido.Id, Orden = i }; p.Detalles.Add(d); }
            if (d.ArbolJson == arbol && d.Cantidad == linea.Cantidad) continue;
            d.Codigo = linea.Codigo; d.Cantidad = linea.Cantidad; d.Precio = linea.PrecioUnitario; d.ArbolJson = arbol; d.Fisicos.Clear();
            foreach (var f in linea.Fisicos()) d.Fisicos.Add(new() { Id = Guid.NewGuid(), DetalleId = d.Id, Sku = f.Sku, Nombre = f.Nombre, Cantidad = f.Cantidad, Precio = f.Precio, PesoKg = f.PesoKg });
        }
        for (int i = p.Historial.Count; i < pedido.Historial.Count; i++)
        {
            var h = pedido.Historial[i]; p.Historial.Add(new() { Id = Guid.NewGuid(), PedidoId = pedido.Id, Orden = i, Desde = h.Desde, Hasta = h.Hasta, Fecha = h.Fecha, Actor = h.Actor });
        }
    }
}
