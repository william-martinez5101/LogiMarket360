using LogiMarket.Application.Contratos;
using LogiMarket.Application.Inventario;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;

namespace LogiMarket.Application.Gestion;

public sealed class CatalogoServicio(IUnidadTrabajoFactory unidades, SesionUsuario sesion, IReloj reloj, DisponibilidadExistencias existencias)
{
    public async Task GuardarProductoAsync(string sku, string nombre, decimal precio, decimal peso, int stockInicial)
    {
        Permisos.Exigir(sesion);
        await using var u = await unidades.AbrirAsync(true);
        sku = sku.Trim().ToUpperInvariant();
        var p = (await u.Catalogo.ProductosAsync()).SingleOrDefault(x => x.Sku == sku);
        if (p is null) p = new(sku, nombre, precio, peso, stockInicial);
        else p.ActualizarDatos(nombre, precio, peso);
        await u.Catalogo.GuardarProductoAsync(p);
        await u.Bitacora.AgregarAsync(new(Guid.NewGuid(), "CATALOGO", sku, $"Producto {nombre} guardado por {sesion.Exigir().Usuario}.", reloj.Ahora));
        await u.ConfirmarAsync();
    }
    public async Task EliminarProductoAsync(string sku)
    {
        Permisos.Exigir(sesion); await using var u = await unidades.AbrirAsync(true);
        await u.Catalogo.EliminarProductoAsync(sku); await u.ConfirmarAsync();
    }
    public async Task GuardarKitAsync(string codigo, string nombre, IReadOnlyList<ComposicionKit> componentes)
    {
        Permisos.Exigir(sesion); await using var u = await unidades.AbrirAsync(true);
        await u.Catalogo.GuardarKitAsync(codigo, nombre, componentes); await u.ConfirmarAsync();
    }
    public async Task EliminarKitAsync(string codigo)
    { Permisos.Exigir(sesion); await using var u = await unidades.AbrirAsync(true); await u.Catalogo.EliminarKitAsync(codigo); await u.ConfirmarAsync(); }
    public async Task AjustarStockAsync(string sku, int variacion, string motivo)
    {
        Permisos.Exigir(sesion, Rol.BODEGA);
        if (variacion == 0 || string.IsNullOrWhiteSpace(motivo)) throw new ArgumentException("Cantidad no nula y motivo obligatorios.");
        await using var u = await unidades.AbrirAsync(true); var productos = await u.Catalogo.ProductosAsync(); var p = productos.Single(x => x.Sku == sku);
        if (variacion < 0)
        {
            var disponibilidad = await existencias.CalcularAsync(productos, await u.Pedidos.ListarAsync(), await u.Operaciones.PagosAsync());
            var libre = disponibilidad.Single(x => x.Sku == sku).Disponible;
            if (checked(-variacion) > libre) throw new InvalidOperationException($"STOCK: El ajuste requiere {checked(-variacion)} de {sku}, disponible {libre}; respete las unidades comprometidas.");
        }
        if (variacion > 0) p.ReintegrarStock(variacion); else p.DescontarStock(checked(-variacion));
        await u.Catalogo.GuardarProductoAsync(p);
        await u.Operaciones.AgregarMovimientoAsync(new(Guid.NewGuid(), null, sku, variacion, "AJUSTE: " + motivo.Trim(), Guid.NewGuid().ToString(), reloj.Ahora));
        await u.ConfirmarAsync();
    }
}
public sealed class ClientesServicio(IUnidadTrabajoFactory unidades, SesionUsuario sesion)
{
    public async Task<Guid> GuardarAsync(Guid? id, string nombre, CategoriaCliente categoria, bool activo)
    {
        Permisos.Exigir(sesion, Rol.VENTAS); await using var u = await unidades.AbrirAsync(true);
        var cliente = id is null ? new Cliente(nombre, categoria) : (await u.Clientes.ListarAsync()).Single(x => x.Id == id);
        cliente.ActualizarDatos(nombre, categoria); if (activo) cliente.Activar(); else cliente.Desactivar();
        await u.Clientes.GuardarAsync(cliente); await u.ConfirmarAsync(); return cliente.Id;
    }
}
