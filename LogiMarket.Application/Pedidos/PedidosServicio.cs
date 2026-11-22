using LogiMarket.Application.Contratos;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Application.Pedidos;

public sealed class PedidosServicio(IUnidadTrabajoFactory unidades, SesionUsuario sesion, IReloj reloj,
    CalculadoraPedido calculadora, IEnumerable<FabricaEntrega> entregas, IDemostracion demo, IIntentosCobro intentos)
{
    public async Task<Guid> RegistrarAsync(PedidoBorrador borrador, Guid? editarId = null, long? versionEsperada = null)
    {
        Permisos.Exigir(sesion, Rol.VENTAS); await using var u = await unidades.AbrirAsync(true);
        var cliente = (await u.Clientes.ListarAsync()).SingleOrDefault(x => x.Id == borrador.ClienteId)
            ?? throw new InvalidOperationException("Seleccione un cliente.");
        var builder = new PedidoBuilder(calculadora, entregas).ParaCliente(cliente).ConEntrega(borrador.Entrega);
        foreach (var linea in borrador.Lineas) builder.AgregarInstantanea(linea);
        var nuevo = builder.Construir(reloj.Ahora, sesion.Exigir().Usuario);
        if (editarId is Guid id)
        {
            var anterior = await ExigirPedido(u, id);
            if (versionEsperada != anterior.Version) throw new InvalidOperationException("El borrador cambio en otra sesion. Recargue.");
            anterior.EditarDesde(nuevo); nuevo = anterior;
        }
        await u.Pedidos.GuardarAsync(nuevo); await u.ConfirmarAsync(); return nuevo.Id;
    }
    public async Task<Guid> ClonarAsync(Guid id)
    {
        Permisos.Exigir(sesion, Rol.VENTAS); await using var u = await unidades.AbrirAsync(true);
        var copia = (await ExigirPedido(u, id)).ClonarBorrador(reloj.Ahora, sesion.Exigir().Usuario);
        await u.Pedidos.GuardarAsync(copia); await u.ConfirmarAsync(); return copia.Id;
    }
    public async Task ValidarAsync(Guid id)
    {
        Permisos.Exigir(sesion, Rol.VENTAS); await using var u = await unidades.AbrirAsync(true);
        var p = await ExigirPedido(u, id); var cliente = (await u.Clientes.ListarAsync()).Single(x => x.Id == p.ClienteId);
        var cadena = new ClienteActivoValidador(); cadena.SeguidoDe(new LineasValidador()).SeguidoDe(new StockValidador());
        var error = cadena.Evaluar(new(cliente, p.Lineas, await u.Catalogo.ProductosAsync(), sesion, p.Estado));
        if (error is not null) throw new InvalidOperationException(error.Mensaje);
        p.Validar(reloj.Ahora, sesion.Exigir().Usuario); await u.Pedidos.GuardarAsync(p); await u.ConfirmarAsync();
    }
    public async Task<string> CancelarAsync(Guid id)
    {
        Permisos.Exigir(sesion, Rol.VENTAS); await using var u = await unidades.AbrirAsync(true);
        var p = await ExigirPedido(u, id);
        if ((await intentos.BuscarAsync(id))?.Aprobacion is not null)
            throw new InvalidOperationException("Hay un cobro aceptado. Concilie confirmando el pedido; si ya esta pagado, utilice devolucion.");
        p.Cancelar(reloj.Ahora, sesion.Exigir().Usuario);
        await u.Pedidos.GuardarAsync(p); await u.ConfirmarAsync(); return "CANCELADO";
    }
    public async Task<DespachoRegistrado> DespacharAsync(Guid id)
    {
        Permisos.Exigir(sesion, Rol.BODEGA); await using var u = await unidades.AbrirAsync(true);
        var p = await ExigirPedido(u, id); p.Despachar(reloj.Ahora, sesion.Exigir().Usuario);
        var despacho = new DespachoRegistrado(id, p.Entrega.Guia, reloj.Ahora, reloj.Ahora.AddDays(p.Entrega.Dias));
        await u.Operaciones.AgregarDespachoAsync(despacho); await u.Pedidos.GuardarAsync(p);
        await u.Eventos.AgregarAsync(new(Guid.NewGuid(), id, "DespachoRegistrado", 0, demo.Canal, reloj.Ahora));
        await u.ConfirmarAsync(); return despacho;
    }
    public async Task EntregarAsync(Guid id)
    {
        Permisos.Exigir(sesion, Rol.BODEGA); await using var u = await unidades.AbrirAsync(true);
        var p = await ExigirPedido(u, id); p.Entregar(reloj.Ahora, sesion.Exigir().Usuario);
        await u.Pedidos.GuardarAsync(p); await u.Bitacora.AgregarAsync(new(Guid.NewGuid(), "ENTREGA", id.ToString(), sesion.Exigir().Usuario, reloj.Ahora));
        await u.ConfirmarAsync();
    }
    internal static async Task<Pedido> ExigirPedido(IUnidadTrabajo u, Guid id)
        => await u.Pedidos.BuscarAsync(id) ?? throw new InvalidOperationException("Pedido inexistente.");
}
