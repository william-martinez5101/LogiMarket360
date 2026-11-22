using System.Text.Json;
using LogiMarket.Application.Contratos;
using LogiMarket.Infrastructure.Persistencia;

namespace LogiMarket.Infrastructure;

public sealed class RelojSistema : IReloj { public DateTimeOffset Ahora => DateTimeOffset.UtcNow; }
public sealed class OpcionesDemostracion : IDemostracion
{
    public string Proveedor { get; set; } = "LEGADO";
    public string Canal { get; set; } = "EMAIL";
    public bool FallarCobro { get; set; }
    public bool FallarNotificacion { get; set; }
    public bool FallarPersistencia { get; set; }
}
public sealed class BitacoraArchivo : IBitacoraTecnica
{
    private readonly string _ruta; private readonly object _cerrojo = new(); private readonly IReloj _reloj;
    public BitacoraArchivo(ConfiguracionBase configuracion, IReloj reloj)
    { Directory.CreateDirectory(configuracion.Directorio); _ruta = Path.Combine(configuracion.Directorio, "operaciones.jsonl"); _reloj = reloj; }
    public void Registrar(string tipo, string correlacion, string mensaje)
    {
        var e = new EntradaBitacora(Guid.NewGuid(), tipo, correlacion, mensaje, _reloj.Ahora);
        lock (_cerrojo) File.AppendAllText(_ruta, JsonSerializer.Serialize(e) + Environment.NewLine);
    }
    public IReadOnlyList<EntradaBitacora> Leer()
    {
        lock (_cerrojo)
            return File.Exists(_ruta) ? File.ReadLines(_ruta).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => JsonSerializer.Deserialize<EntradaBitacora>(l)!).ToList() : [];
    }
}
