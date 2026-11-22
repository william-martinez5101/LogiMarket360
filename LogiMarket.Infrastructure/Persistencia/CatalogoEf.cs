using LogiMarket.Application.Contratos;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia;

internal sealed class CatalogoEf(LogiMarketDbContext db) : ICatalogoRepository, IClienteRepository
{
    public async Task<IReadOnlyList<ProductoSimple>> ProductosAsync() => await db.Set<ProductoSimple>().OrderBy(x => x.Sku).ToListAsync();
    public async Task<IReadOnlyList<KitCompuesto>> KitsAsync()
    {
        var productos = (await ProductosAsync()).ToDictionary(x => x.Id);
        var filas = await db.Set<KitFila>().Include(x => x.Componentes).OrderBy(x => x.Codigo).ToListAsync();
        var terminados = new Dictionary<Guid, KitCompuesto>(); var activos = new HashSet<Guid>();
        KitCompuesto Construir(Guid id)
        {
            if (terminados.TryGetValue(id, out var previo)) return previo;
            if (!activos.Add(id)) throw new InvalidOperationException("Ciclo detectado en kits persistidos.");
            var fila = filas.Single(x => x.Id == id); var kit = new KitCompuesto(fila.Codigo, fila.Nombre);
            foreach (var c in fila.Componentes.OrderBy(x => x.Orden))
                kit.Agregar(c.ProductoId is Guid pid ? productos[pid] : Construir(c.SubkitId!.Value), c.Cantidad);
            activos.Remove(id); terminados[id] = kit; return kit;
        }
        return filas.Select(x => Construir(x.Id)).ToList();
    }
    public async Task GuardarProductoAsync(ProductoSimple producto)
    {
        if (await db.Set<KitFila>().AnyAsync(x => x.Codigo == producto.Sku)) throw new InvalidOperationException("Codigo usado por un kit.");
        if (db.Entry(producto).State == EntityState.Detached) db.Add(producto);
    }
    public async Task EliminarProductoAsync(string sku)
    {
        var producto = await db.Set<ProductoSimple>().SingleAsync(x => x.Sku == sku);
        if (await db.Set<KitComponenteFila>().AnyAsync(x => x.ProductoId == producto.Id)
            || await db.Set<FisicoFila>().AnyAsync(x => x.Sku == sku) || await db.Set<MovimientoFila>().AnyAsync(x => x.Sku == sku))
            throw new InvalidOperationException("Producto referenciado por kits, ventas o movimientos; no se puede eliminar.");
        db.Remove(producto);
    }
    public async Task GuardarKitAsync(string codigo, string nombre, IReadOnlyList<ComposicionKit> componentes)
    {
        codigo = codigo.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre) || componentes.Count == 0)
            throw new ArgumentException("Codigo, nombre y componentes obligatorios.");
        var productos = await ProductosAsync();
        if (productos.Any(x => x.Sku == codigo)) throw new InvalidOperationException("Codigo usado por un producto.");
        var kits = await db.Set<KitFila>().Include(x => x.Componentes).ToListAsync();
        var fila = kits.SingleOrDefault(x => x.Codigo == codigo);
        if (fila is null) { fila = new() { Id = Guid.NewGuid(), Codigo = codigo }; kits.Add(fila); db.Add(fila); }
        var nuevas = componentes.Select((c, orden) => {
            var p = productos.SingleOrDefault(x => x.Sku == c.Codigo);
            var k = kits.SingleOrDefault(x => x.Codigo == c.Codigo);
            if (p is null && k is null) throw new InvalidOperationException($"Componente desconocido: {c.Codigo}.");
            return new KitComponenteFila { Id = Guid.NewGuid(), KitId = fila.Id, ProductoId = p?.Id, SubkitId = k?.Id, Cantidad = Cantidades.Validar(c.Cantidad), Orden = orden };
        }).ToList();
        var visitando = new HashSet<Guid>(); var visitados = new HashSet<Guid>();
        void Validar(Guid id)
        {
            if (visitados.Contains(id)) return;
            if (!visitando.Add(id)) throw new InvalidOperationException("El componente generaria una referencia circular.");
            foreach (var c in id == fila.Id ? nuevas : kits.Single(x => x.Id == id).Componentes)
                if (c.SubkitId is Guid sub) Validar(sub);
            visitando.Remove(id); visitados.Add(id);
        }
        Validar(fila.Id); fila.Nombre = nombre.Trim(); fila.Componentes.Clear(); fila.Componentes.AddRange(nuevas);
    }
    public async Task EliminarKitAsync(string codigo)
    {
        var kit = await db.Set<KitFila>().SingleAsync(x => x.Codigo == codigo);
        if (await db.Set<KitComponenteFila>().AnyAsync(x => x.SubkitId == kit.Id)) throw new InvalidOperationException("Kit usado por otro kit.");
        db.Remove(kit);
    }
    public async Task<IReadOnlyList<Cliente>> ListarAsync() => await db.Set<Cliente>().OrderBy(x => x.Nombre).ToListAsync();
    public Task GuardarAsync(Cliente cliente)
    { if (db.Entry(cliente).State == EntityState.Detached) db.Add(cliente); return Task.CompletedTask; }
}
