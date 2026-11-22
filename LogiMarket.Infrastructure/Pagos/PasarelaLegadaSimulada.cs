using LogiMarket.Application.Contratos;

namespace LogiMarket.Infrastructure.Pagos;

// API legada incompatible, conservada independientemente del contrato de Application.
public sealed class PasarelaLegadaSimulada(DiarioCobrosSimulados diario, IDemostracion demo, IBitacoraTecnica bitacora)
{
    public RespuestaLegada CobrarCentavos(long centavos, string clave)
    {
        if (centavos <= 0) throw new ArgumentException("Centavos invalidos.");
        bitacora.Registrar("PASARELA", clave, $"LEGADO: {centavos} centavos");
        if (demo.FallarCobro) return new(false, "", "LEGADO", "Rechazo simulado.");
        var r = diario.Aprobar(centavos / 100m, clave, "LEGADO"); return new(r.Aprobado, r.Referencia, r.Proveedor, r.Mensaje);
    }
}
public sealed record RespuestaLegada(bool Exito, string Referencia, string Proveedor, string Detalle);
