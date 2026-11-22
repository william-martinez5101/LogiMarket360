using LogiMarket.Application.Contratos;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LogiMarket.Infrastructure.Persistencia;

public sealed class UnidadTrabajoEfFactory(ConfiguracionBase configuracion, IDemostracion demo) : IUnidadTrabajoFactory
{
    public async Task<IUnidadTrabajo> AbrirAsync(bool escritura, CancellationToken ct = default)
    {
        var db = configuracion.CrearContexto();
        try
        {
            IDbContextTransaction? transaccion = null;
            if (escritura)
            {
                await db.Database.OpenConnectionAsync(ct);
                var conexion = (SqliteConnection)db.Database.GetDbConnection();
                // BEGIN IMMEDIATE serializa escritores incluso entre procesos/conexiones independientes.
                var nativa = conexion.BeginTransaction(deferred: false);
                transaccion = await db.Database.UseTransactionAsync(nativa, ct);
            }
            return new UnidadTrabajoEf(db, transaccion, escritura, demo);
        }
        catch { await db.DisposeAsync(); throw; }
    }
}
internal sealed class UnidadTrabajoEf : IUnidadTrabajo
{
    private readonly LogiMarketDbContext _db;
    private readonly IDbContextTransaction? _transaccion;
    private readonly bool _escritura;
    private readonly IDemostracion _demo;
    public ICatalogoRepository Catalogo { get; }
    public IClienteRepository Clientes { get; }
    public IPedidoRepository Pedidos { get; }
    public IOperacionRepository Operaciones { get; }
    public IEventoRepository Eventos { get; }
    public IBitacoraRepository Bitacora { get; }
    public UnidadTrabajoEf(LogiMarketDbContext db, IDbContextTransaction? transaccion, bool escritura, IDemostracion demo)
    {
        _db = db; _transaccion = transaccion; _escritura = escritura; _demo = demo;
        var catalogo = new CatalogoEf(db); Catalogo = catalogo; Clientes = catalogo; Pedidos = new PedidosEf(db);
        var operaciones = new OperacionesEf(db); Operaciones = operaciones; Eventos = operaciones; Bitacora = operaciones;
    }
    public async Task ConfirmarAsync(CancellationToken ct = default)
    {
        if (!_escritura) throw new InvalidOperationException("Unidad de consulta sin permiso de escritura.");
        if (_demo.FallarPersistencia && _db.ChangeTracker.Entries<PagoFila>().Any(x => x.State == EntityState.Added))
            throw new IOException("Fallo de persistencia del cobro inducido despues de la aceptacion y antes del commit. Reintente confirmar para conciliar.");
        await _db.SaveChangesAsync(ct);
        await _transaccion!.CommitAsync(ct);
    }
    public async ValueTask DisposeAsync()
    { if (_transaccion is not null) await _transaccion.DisposeAsync(); await _db.DisposeAsync(); }
}
