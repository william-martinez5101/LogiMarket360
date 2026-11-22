using LogiMarket.Domain.Clientes;
using LogiMarket.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Tests.Integracion;

public sealed class RegresionesSeedRevisionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReinicioConOSinMarcaPrevia_ConservaBajasEdicionesYOperaciones(bool legado)
    {
        await using var e = await Escenario.CrearAsync(); await e.PagadoAsync(); await e.Outbox.ProcesarAsync();
        var ana = (await e.Consultas.CargarAsync()).Clientes.Single(x => x.Nombre == "Ana");
        await e.Clientes.GuardarAsync(ana.Id, "Ana renombrada", CategoriaCliente.Normal, false);
        await e.Catalogo.EliminarProductoAsync("P04"); await e.Catalogo.EliminarKitAsync("KIT_TELETRABAJO");
        await e.Catalogo.GuardarProductoAsync("P05", "Nombre editado", 7, .3m, 999);
        await using (var db = e.Config.CrearContexto())
            if (legado) await db.Database.ExecuteSqlRawAsync("DELETE FROM Auditorias WHERE Tipo = 'INICIALIZACION'");
        var antes = await e.Consultas.CargarAsync();
        await using var r = new Escenario(e.Config.Ruta); await r.InicializarAsync(); await r.InicializarAsync();
        var d = await r.Consultas.CargarAsync(); Assert.DoesNotContain(d.Productos, x => x.Sku == "P04");
        Assert.DoesNotContain(d.Kits, x => x.Codigo == "KIT_TELETRABAJO"); Assert.Equal(3, d.Clientes.Count);
        var renombrada = d.Clientes.Single(x => x.Id == ana.Id); Assert.Equal("Ana renombrada", renombrada.Nombre); Assert.False(renombrada.Activo);
        Assert.DoesNotContain(d.Clientes, x => x.Nombre == "Ana"); var p = d.Productos.Single(x => x.Sku == "P05");
        Assert.Equal("Nombre editado", p.Nombre); Assert.Equal(7, p.Precio); Assert.Equal(30, p.Stock);
        Assert.Equal(antes.Pagos, d.Pagos); Assert.Equal(antes.Movimientos, d.Movimientos); Assert.Equal(antes.Avisos, d.Avisos);
        Assert.Equal(antes.Pedidos.Single().Valores, d.Pedidos.Single().Valores);
        Assert.Single(d.Auditoria.Where(x => x.Tipo == "INICIALIZACION"));
        await r.EvidenciaAsync("R02-" + (legado ? "LEGADO" : "MARCA"), "Bajas y ediciones conservadas tras dos inicializaciones; operaciones y snapshots intactos", new { p, Clientes = d.Clientes, Kits = d.Kits.Select(x => x.Codigo) });
    }

    [Fact]
    public async Task InicializacionInterrumpida_RevierteTodoYPermiteReintentarDesdeCero()
    {
        await using var e = new Escenario();
        await using (var db = e.Config.CrearContexto())
        {
            await db.Database.MigrateAsync();
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER InterrumpirSeed BEFORE INSERT ON Kits BEGIN SELECT RAISE(ABORT, 'Interrupcion real del seed'); END;");
        }
        await Assert.ThrowsAsync<DbUpdateException>(() => new InicializadorBase(e.Config, e.Hash).MigrarYSeedAsync());
        await using (var db = e.Config.CrearContexto())
        {
            Assert.Empty(await db.Set<LogiMarket.Domain.Catalogo.ProductoSimple>().ToListAsync());
            Assert.Empty(await db.Set<Cliente>().ToListAsync());
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER InterrumpirSeed;");
        }
        await e.InicializarAsync(); await e.InicializarAsync(); var d = await e.Consultas.CargarAsync();
        Assert.Equal(5, d.Productos.Count); Assert.Equal(2, d.Kits.Count); Assert.Equal(3, d.Clientes.Count);
        Assert.Single(d.Auditoria.Where(x => x.Tipo == "INICIALIZACION"));
        await e.EvidenciaAsync("R02-INTERRUPCION", "Fallo real de SQLite en kits revierte la siembra completa; reintento sin duplicados", new { Productos = d.Productos.Count, Kits = d.Kits.Count });
    }
}
