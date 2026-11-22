using System.Xml.Linq;
using LogiMarket.Application.Contratos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Domain.Ventas;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Tests.Integracion;

public sealed partial class AceptacionTests
{
    [Fact] public async Task P35_DevolucionUnica54SinFlete()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.EntregadoAsync();
        var dev = await e.Invocador.EjecutarAsync(new ProcesarDevolucionCommand(e.Devoluciones, new(id, "DEFECTO", true)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Invocador.EjecutarAsync(new ProcesarDevolucionCommand(e.Devoluciones, new(id, "DEFECTO", true))));
        var d = await e.Consultas.CargarAsync(); Assert.Equal(54, dev.Reembolso); Assert.Single(d.Devoluciones); Assert.Equal(6, d.Movimientos.Count);
        Assert.Equal(new[] { 8, 5, 20, 4, 30 }, d.Productos.Select(x => x.Stock)); Assert.Equal(1, d.Outbox.Count(x => x.Evento.Tipo == "DevolucionAceptada"));
        await e.EvidenciaAsync("P35", "54.00 sin flete; una devolucion y un reintegro por SKU; repeticion rechazada", dev);
    }
    [Fact] public async Task P36_ConcurrenciaDosConexionesUnStock()
    {
        await using var e = await Escenario.CrearAsync(); await e.Catalogo.AjustarStockAsync("P01", -7, "P36 stock controlado=1");
        var p1 = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P01", 1)); var p2 = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P01", 1));
        await using var otra = new Escenario(e.Config.Ruta); await otra.ComoAsync(Rol.VENTAS);
        using var inicio = new ManualResetEventSlim(false);
        Task<(bool Exito, string Resultado)> Competir(Escenario esc, Guid id, string clave) => Task.Run(async () => {
            inicio.Wait(); try { var p = await esc.Facade.ConfirmarAsync(id, clave); return (true, p.Referencia); }
            catch (InvalidOperationException ex) { return (false, ex.Message); }
        });
        var t1 = Competir(e, p1, "concurrente-1"); var t2 = Competir(otra, p2, "concurrente-2"); inicio.Set(); var resultados = await Task.WhenAll(t1, t2);
        var d = await e.Consultas.CargarAsync(); Assert.Equal(1, resultados.Count(x => x.Exito)); Assert.Single(d.Pagos); Assert.Single(e.Diario.Leer());
        Assert.Equal(0, d.Productos.Single(x => x.Sku == "P01").Stock); Assert.Single(d.Outbox); Assert.Single(d.Movimientos.Where(x => x.Motivo == "VENTA"));
        await e.EvidenciaAsync("P36", "Dos servicios/conexiones concurrentes: maximo un cobro aprobado; stock=0", resultados.Select(x => new { x.Exito, x.Resultado }));
    }
    [Fact] public async Task P37_OutboxRecuperaTrasReinicioSinDuplicados()
    {
        await using var e = await Escenario.CrearAsync(); await e.PagadoAsync(); e.Demo.FallarNotificacion = true; var fallo = await e.Outbox.ProcesarAsync();
        var antes = await e.Consultas.CargarAsync(); Assert.Equal(1, fallo.Pendientes); Assert.Empty(antes.Avisos); Assert.Equal(1, e.Metricas.Pagos); Assert.Single(antes.Auditoria.Where(x => x.Tipo == "EVENTO"));
        await using var r = new Escenario(e.Config.Ruta); await r.InicializarAsync(); var recuperado = await r.Outbox.ProcesarAsync(); await r.Outbox.ProcesarAsync();
        var despues = await r.Consultas.CargarAsync(); Assert.Equal(0, recuperado.Pendientes); Assert.Single(despues.Avisos); Assert.Equal(1, r.Metricas.Pagos);
        Assert.Single(despues.Auditoria.Where(x => x.Tipo == "EVENTO")); Assert.Single(despues.Pagos); Assert.Single(r.Diario.Leer()); Assert.NotNull(despues.Outbox.Single().Procesado); Assert.Equal(2, despues.Outbox.Single().Intentos);
        await r.EvidenciaAsync("P37", "Pendiente recuperado con nuevo procesador; un aviso/auditoria/metrica/pago/cobro", new { fallo, recuperado, Intentos = despues.Outbox.Single().Intentos, AntesAvisos = antes.Avisos.Count, DespuesAvisos = despues.Avisos.Count });
    }
    [Fact] public async Task P38_ArquitecturaLspIspDipNet8()
    {
        await using var e = await Escenario.CrearAsync();
        var raiz = new DirectoryInfo(AppContext.BaseDirectory); while (raiz is not null && !File.Exists(Path.Combine(raiz.FullName, "LogiMarket360.slnx"))) raiz = raiz.Parent;
        Assert.NotNull(raiz);
        foreach (var proyecto in new[] { "Domain", "Application", "Infrastructure", "WinForms", "Tests" })
        {
            var archivo = Path.Combine(raiz!.FullName, "LogiMarket." + proyecto, "LogiMarket." + proyecto + ".csproj"); var xml = XDocument.Load(archivo);
            Assert.Equal(proyecto == "WinForms" ? "net8.0-windows" : "net8.0", xml.Descendants("TargetFramework").Single().Value);
            if (proyecto is "Domain" or "Application")
                Assert.DoesNotContain(xml.Descendants("PackageReference"), p => (p.Attribute("Include")?.Value ?? "").Contains("EntityFramework", StringComparison.OrdinalIgnoreCase) || (p.Attribute("Include")?.Value ?? "").Contains("Sqlite", StringComparison.OrdinalIgnoreCase));
            if (proyecto == "Domain") Assert.Empty(xml.Descendants("ProjectReference"));
            if (proyecto == "Application") Assert.Single(xml.Descendants("ProjectReference"));
        }
        foreach (var tipo in new[] { "LEGADO", "ALTERNATIVO" })
        {
            e.Demo.Proveedor = tipo;
            await Assert.ThrowsAsync<ArgumentException>(() => e.Pasarela.CobrarAsync(.001m, "lsp"));
            await Assert.ThrowsAsync<ArgumentException>(() => e.Pasarela.CobrarAsync(1, ""));
            await Assert.ThrowsAsync<OperationCanceledException>(() => e.Pasarela.CobrarAsync(1, "cancelado", new CancellationToken(true)));
            var r = await e.Pasarela.CobrarAsync(1, "lsp-" + tipo); Assert.True(r.Aprobado); Assert.NotEmpty(r.Referencia); Assert.Equal(tipo, r.Proveedor);
        }
        Assert.True(typeof(IPasarelaPago).GetMethods().Length == 1); Assert.True(typeof(IHashContrasena).GetMethods().Length == 2);
        await e.EvidenciaAsync("P38", "Referencias y TFM correctos; contratos segregados; proveedores sustituibles en entradas, resultados y excepciones; build limpio en log externo", new { Framework = ".NET 8", Proveedores = e.Diario.Leer(), ContratoPagoMetodos = typeof(IPasarelaPago).GetMethods().Length });
    }
    [Fact] public async Task RestriccionesSqliteReales_NoStockNegativoNiSkuDuplicadoNiHuerfanos()
    {
        await using var e = await Escenario.CrearAsync(); await e.PagadoAsync(); await using var db = e.Config.CrearContexto();
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync("UPDATE Productos SET Stock = -1 WHERE Sku = 'P01'"));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync("UPDATE Productos SET Sku = 'P02' WHERE Sku = 'P01'"));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync("DELETE FROM Clientes"));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync("INSERT INTO Pagos SELECT '11111111-1111-1111-1111-111111111111',PedidoId,Clave,Referencia,Importe,Proveedor,Fecha FROM Pagos"));
        Assert.Single((await e.Consultas.CargarAsync()).Pagos);
    }
    [Fact] public async Task SnapshotConservaPrecioYComposicionTrasEditarCatalogo()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.PagadoAsync(); await e.Catalogo.GuardarProductoAsync("P01", "Mouse actualizado", 99, 2, 100);
        await e.Catalogo.GuardarKitAsync("KIT_TELETRABAJO", "Kit actualizado", [new("P05", 5)]);
        var p = (await e.Consultas.CargarAsync()).Pedidos.Single(); Assert.Equal(59, p.Valores.Total); Assert.Equal(new[] { "P01", "P02", "P03" }, p.Fisicos().Select(x => x.Sku));
        Assert.Equal(20, p.Lineas[0].Elemento.Hijos[0].Hijos[0].Precio);
    }
    [Fact] public async Task SkusRepetidosSeAgrupanAntesDeValidarYMover()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.PagadoAsync("Luis", TipoEntrega.ESTANDAR, ("KIT_TELETRABAJO", 1), ("P01", 2));
        var d = await e.Consultas.CargarAsync(); Assert.Equal(3, d.Movimientos.Count); Assert.Equal(-3, d.Movimientos.Single(x => x.Sku == "P01").Cantidad); Assert.Equal(5, d.Productos.Single(x => x.Sku == "P01").Stock);
    }
    [Theory] [InlineData("DEFECTO", 30, false, true)] [InlineData("DEFECTO", 31, true, false)]
    [InlineData("ARREPENTIMIENTO", 7, true, true)] [InlineData("ARREPENTIMIENTO", 8, true, false)] [InlineData("ARREPENTIMIENTO", 0, false, false)]
    public async Task DevolucionLimites(string motivo, int dias, bool sinUso, bool acepta)
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.EntregadoAsync(); e.Reloj.Ahora = e.Reloj.Ahora.AddDays(dias);
        if (acepta) Assert.Equal(54, (await e.Devoluciones.ProcesarAsync(new(id, motivo, sinUso))).Reembolso);
        else { await Assert.ThrowsAsync<InvalidOperationException>(() => e.Devoluciones.ProcesarAsync(new(id, motivo, sinUso))); Assert.Empty((await e.Consultas.CargarAsync()).Devoluciones); }
    }
    [Fact] public async Task AccesoHashYPermisosSinInterfaz()
    {
        await using var e = await Escenario.CrearAsync(); var h1 = e.Hash.Crear("Demo360!"); var h2 = e.Hash.Crear("Demo360!"); Assert.NotEqual(h1, h2); Assert.DoesNotContain("Demo360!", h1); Assert.True(e.Hash.Verificar("Demo360!", h1)); Assert.False(e.Hash.Verificar("incorrecta", h1));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => e.Acceso.IngresarAsync("admin.demo", "error"));
        await e.ComoAsync(Rol.CONSULTA); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => e.Catalogo.AjustarStockAsync("P01", 1, "denegado")); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => e.Clientes.GuardarAsync(null, "No creado", LogiMarket.Domain.Clientes.CategoriaCliente.Normal, true));
        e.Acceso.CerrarSesion(); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => e.Consultas.CargarAsync());
    }
}
