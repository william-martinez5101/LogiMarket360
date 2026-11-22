using LogiMarket.Application.Contratos;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia;

public sealed class InicializadorBase(ConfiguracionBase configuracion, IHashContrasena hash)
{
    private static readonly Guid MarcaDatosV1 = new("c3c36000-2026-4000-8000-000000000001");
    public async Task MigrarYSeedAsync()
    {
        await using var db = configuracion.CrearContexto();
        await db.Database.MigrateAsync();
        await db.Database.OpenConnectionAsync();
        using var bloqueo = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: false);
        await using var tx = await db.Database.UseTransactionAsync(bloqueo);
        if (await db.Set<BitacoraFila>().AnyAsync(x => x.Id == MarcaDatosV1))
        { await tx!.CommitAsync(); return; }
        // La version anterior tambien sembraba en una sola transaccion. Una base ya en uso
        // se adopta tal como esta: no se infieren bajas por ausencia ni clientes por su nombre.
        var existente = await db.Set<UsuarioFila>().AnyAsync() || await db.Set<ProductoSimple>().AnyAsync()
            || await db.Set<Cliente>().AnyAsync() || await db.Set<KitFila>().AnyAsync() || await db.Set<PedidoFila>().AnyAsync();
        db.Add(new BitacoraFila { Id = MarcaDatosV1, Tipo = "INICIALIZACION", Correlacion = "DATOS_DEMO_V1",
            Mensaje = existente ? "Base existente adoptada sin reponer ni modificar datos." : "Datos oficiales V1 inicializados una sola vez.", Fecha = DateTimeOffset.UtcNow });
        if (existente) { await db.SaveChangesAsync(); await tx!.CommitAsync(); return; }
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
