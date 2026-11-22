using System.Text.Json;
using LogiMarket.Application.Contratos;
using LogiMarket.Infrastructure.Persistencia;

namespace LogiMarket.Infrastructure.Pagos;

public sealed record CobroSimulado(string Clave, decimal Monto, string Referencia, string Proveedor, Guid? PedidoId = null);
// El proveedor guarda sus aceptaciones fuera de la transaccion local que puede revertirse.
public sealed class DiarioCobrosSimulados(ConfiguracionBase configuracion) : IIntentosCobro
{
    private sealed record Reserva(Guid PedidoId, decimal Importe, List<string> Claves);
    private string RutaCobros => Path.Combine(configuracion.Directorio, "cobros-simulados.json");
    private string RutaIntentos => Path.Combine(configuracion.Directorio, "intentos-cobro.json");
    private FileStream Bloquear(CancellationToken ct = default)
    {
        Directory.CreateDirectory(configuracion.Directorio);
        var espera = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try { return new FileStream(RutaCobros + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (espera.Elapsed < TimeSpan.FromSeconds(5))
            { Thread.Sleep(10); } // Consultas de otra ventana tambien leen el diario bajo este bloqueo.
        }
    }
    private static List<T> LeerArchivo<T>(string ruta)
        => File.Exists(ruta) ? JsonSerializer.Deserialize<List<T>>(File.ReadAllText(ruta))
            ?? throw new IOException("Diario de cobros invalido; requiere revision.") : [];
    private static void Guardar<T>(string ruta, List<T> valores)
    {
        var temporal = ruta + ".tmp";
        using (var archivo = new FileStream(temporal, FileMode.Create, FileAccess.Write, FileShare.None))
        { JsonSerializer.Serialize(archivo, valores); archivo.Flush(flushToDisk: true); }
        File.Move(temporal, ruta, overwrite: true);
    }
    private static RespuestaCobro Respuesta(CobroSimulado cobro)
        => new(true, cobro.Referencia, cobro.Proveedor, "Resultado original recuperado.");

    public Task<IntentoCobro> PrepararAsync(Guid pedidoId, string clave, decimal importe, IReadOnlyCollection<string> referenciasConfirmadas, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested(); ValidacionCobro.Exigir(importe, clave);
        if (pedidoId == Guid.Empty) throw new ArgumentException("Pedido obligatorio.");
        using var cerrojo = Bloquear(ct);
        var reservas = LeerArchivo<Reserva>(RutaIntentos); var cobros = LeerArchivo<CobroSimulado>(RutaCobros);
        // Un diario antiguo sin PedidoId no permite atribuir con certeza una aceptacion
        // huerfana. Se exige revision en vez de arriesgar otro cargo con una nueva clave.
        if (cobros.Any(x => x.PedidoId is null && !referenciasConfirmadas.Contains(x.Referencia)))
            throw new InvalidOperationException("Hay un cobro de una version anterior sin conciliacion. Revise el diario y los pagos antes de realizar nuevos cargos.");
        var propietario = reservas.SingleOrDefault(x => x.Claves.Contains(clave));
        if (propietario is not null && propietario.PedidoId != pedidoId)
            throw new InvalidOperationException("La clave ya pertenece a otro pedido, incluso si se revirtio su guardado local.");
        var cobroPorClave = cobros.SingleOrDefault(x => x.Clave == clave);
        if (cobroPorClave is not null && cobroPorClave.PedidoId != pedidoId)
            throw new InvalidOperationException("La clave tiene un cobro previo sin asociacion a este pedido; requiere conciliacion.");
        var reserva = reservas.SingleOrDefault(x => x.PedidoId == pedidoId);
        if (reserva is null) { reserva = new(pedidoId, importe, []); reservas.Add(reserva); }
        if (reserva.Importe != importe) throw new InvalidOperationException("El intento del pedido tiene otro importe; requiere conciliacion.");
        if (!reserva.Claves.Contains(clave)) { reserva.Claves.Add(clave); Guardar(RutaIntentos, reservas); }
        var aceptado = cobros.SingleOrDefault(x => x.PedidoId == pedidoId);
        if (aceptado is not null && aceptado.Monto != importe) throw new InvalidOperationException("El cobro aceptado tiene otro importe.");
        return Task.FromResult(new IntentoCobro(pedidoId, importe, aceptado is null ? null : Respuesta(aceptado)));
    }
    public Task<IntentoCobro?> BuscarAsync(Guid pedidoId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested(); using var cerrojo = Bloquear(ct);
        var reserva = LeerArchivo<Reserva>(RutaIntentos).SingleOrDefault(x => x.PedidoId == pedidoId);
        var cobro = LeerArchivo<CobroSimulado>(RutaCobros).SingleOrDefault(x => x.PedidoId == pedidoId);
        return Task.FromResult<IntentoCobro?>(reserva is null ? null : new(pedidoId, reserva.Importe, cobro is null ? null : Respuesta(cobro)));
    }
    public RespuestaCobro Aprobar(decimal monto, string clave, string proveedor)
    {
        ValidacionCobro.Exigir(monto, clave); using var cerrojo = Bloquear();
        var reserva = LeerArchivo<Reserva>(RutaIntentos).SingleOrDefault(x => x.Claves.Contains(clave));
        if (reserva is not null && reserva.Importe != monto) throw new InvalidOperationException("Importe distinto del intento reservado.");
        var registros = LeerArchivo<CobroSimulado>(RutaCobros);
        var previo = registros.SingleOrDefault(x => reserva is null ? x.Clave == clave : x.PedidoId == reserva.PedidoId);
        if (previo is not null)
        {
            if (previo.Monto != monto) throw new InvalidOperationException("Clave reutilizada con otro importe.");
            return Respuesta(previo);
        }
        var cobro = new CobroSimulado(clave, monto, $"SIM-{Guid.NewGuid():N}", proveedor, reserva?.PedidoId); registros.Add(cobro);
        Guardar(RutaCobros, registros);
        return new(true, cobro.Referencia, proveedor, "Cobro simulado aprobado.");
    }
    public Task<IReadOnlyList<IntentoCobro>> AceptadosAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var aceptados = Leer(ct).Where(x => x.PedidoId.HasValue)
            .Select(x => new IntentoCobro(x.PedidoId!.Value, x.Monto, Respuesta(x))).ToList();
        if (aceptados.GroupBy(x => x.PedidoId).Any(g => g.Count() != 1))
            throw new InvalidOperationException("Hay mas de una aceptacion para un pedido; requiere conciliacion.");
        return Task.FromResult<IReadOnlyList<IntentoCobro>>(aceptados);
    }
    public IReadOnlyList<CobroSimulado> Leer(CancellationToken ct = default)
    {
        using var cerrojo = Bloquear(ct); return LeerArchivo<CobroSimulado>(RutaCobros);
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
