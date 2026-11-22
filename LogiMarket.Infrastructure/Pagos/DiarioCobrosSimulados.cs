using System.Text.Json;
using LogiMarket.Application.Contratos;
using LogiMarket.Infrastructure.Persistencia;

namespace LogiMarket.Infrastructure.Pagos;

public sealed record CobroSimulado(string Clave, decimal Monto, string Referencia, string Proveedor);
// Diario durable del proveedor simulado. La misma clave sobrevive a rollback/reinicio local.
public sealed class DiarioCobrosSimulados(ConfiguracionBase configuracion)
{
    public RespuestaCobro Aprobar(decimal monto, string clave, string proveedor)
    {
        Directory.CreateDirectory(configuracion.Directorio);
        var ruta = Path.Combine(configuracion.Directorio, "cobros-simulados.json");
        using var cerrojo = new FileStream(ruta + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var registros = File.Exists(ruta) ? JsonSerializer.Deserialize<List<CobroSimulado>>(File.ReadAllText(ruta))! : [];
        var previo = registros.SingleOrDefault(x => x.Clave == clave);
        if (previo is not null)
        {
            if (previo.Monto != monto) throw new InvalidOperationException("Clave reutilizada con otro importe.");
            return new(true, previo.Referencia, previo.Proveedor, "Resultado original recuperado.");
        }
        var cobro = new CobroSimulado(clave, monto, $"SIM-{Guid.NewGuid():N}", proveedor); registros.Add(cobro);
        var temporal = ruta + ".tmp";
        using (var archivo = new FileStream(temporal, FileMode.Create, FileAccess.Write, FileShare.None))
        { JsonSerializer.Serialize(archivo, registros); archivo.Flush(flushToDisk: true); }
        File.Move(temporal, ruta, overwrite: true);
        return new(true, cobro.Referencia, proveedor, "Cobro simulado aprobado.");
    }
    public IReadOnlyList<CobroSimulado> Leer()
    {
        var ruta = Path.Combine(configuracion.Directorio, "cobros-simulados.json");
        return File.Exists(ruta) ? JsonSerializer.Deserialize<List<CobroSimulado>>(File.ReadAllText(ruta))! : [];
    }
}
public static class ValidacionCobro
{
    public static void Exigir(decimal monto, string clave)
    {
        if (monto <= 0 || decimal.Round(monto, 2, MidpointRounding.AwayFromZero) != monto)
            throw new ArgumentException("El monto debe ser positivo y tener como maximo dos decimales.", nameof(monto));
        if (string.IsNullOrWhiteSpace(clave) || clave.Length > 120) throw new ArgumentException("Clave invalida.", nameof(clave));
        _ = checked((long)(monto * 100));
    }
}
