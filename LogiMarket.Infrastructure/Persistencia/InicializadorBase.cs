using LogiMarket.Application.Contratos;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia;

public sealed class InicializadorBase(ConfiguracionBase configuracion, IHashContrasena hash)
{
    public async Task MigrarYSeedAsync()
    {
        await using var db = configuracion.CrearContexto();
        await db.Database.MigrateAsync();
        await db.Database.OpenConnectionAsync();
        using var bloqueo = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: false);
        await using var tx = await db.Database.UseTransactionAsync(bloqueo);
        var datos = new[] { ("P01", "Mouse USB", 20m, .20m, 8), ("P02", "Teclado", 30m, .70m, 5),
            ("P03", "Memoria USB", 10m, .10m, 20), ("P04", "Webcam", 40m, .35m, 4), ("P05", "Alfombrilla", 5m, .10m, 30) };
        foreach (var (sku, nombre, precio, peso, stock) in datos)
            if (!await db.Set<ProductoSimple>().AnyAsync(x => x.Sku == sku)) db.Add(new ProductoSimple(sku, nombre, precio, peso, stock));
        foreach (var (nombre, categoria) in new[] { ("Ana", CategoriaCliente.Vip), ("Luis", CategoriaCliente.Normal), ("Carmen", CategoriaCliente.Mayorista) })
            if (!await db.Set<Cliente>().AnyAsync(x => x.Nombre == nombre)) db.Add(new Cliente(nombre, categoria));
        foreach (var rol in Enum.GetValues<Rol>())
        {
            var nombre = rol.ToString().ToLowerInvariant() + ".demo";
            if (!await db.Set<UsuarioFila>().AnyAsync(x => x.Nombre == nombre))
                db.Add(new UsuarioFila { Id = Guid.NewGuid(), Nombre = nombre, Rol = rol, Hash = hash.Crear("Demo360!") });
        }
        await db.SaveChangesAsync();
        var repo = new CatalogoEf(db);
        if (!await db.Set<KitFila>().AnyAsync(x => x.Codigo == "KIT_OFICINA"))
        { await repo.GuardarKitAsync("KIT_OFICINA", "Kit oficina", [new("P01", 1), new("P02", 1)]); await db.SaveChangesAsync(); }
        if (!await db.Set<KitFila>().AnyAsync(x => x.Codigo == "KIT_TELETRABAJO"))
        { await repo.GuardarKitAsync("KIT_TELETRABAJO", "Kit teletrabajo", [new("KIT_OFICINA", 1), new("P03", 1)]); await db.SaveChangesAsync(); }
        await tx!.CommitAsync();
    }
}
