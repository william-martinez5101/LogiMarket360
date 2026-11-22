using LogiMarket.Application.Contratos;

namespace LogiMarket.Infrastructure.Pagos;

public sealed class PasarelaLegadaAdapter(PasarelaLegadaSimulada legado) : IPasarelaPago
{
    public Task<RespuestaCobro> CobrarAsync(decimal monto, string clave, CancellationToken ct = default)
    {
        ValidacionCobro.Exigir(monto, clave); ct.ThrowIfCancellationRequested();
        var r = legado.CobrarCentavos(checked((long)(monto * 100)), clave);
        return Task.FromResult(new RespuestaCobro(r.Exito, r.Referencia, r.Proveedor, r.Detalle));
    }
}
public sealed class PasarelaAlternativa(DiarioCobrosSimulados diario, IDemostracion demo, IBitacoraTecnica bitacora) : IPasarelaPago
{
    public Task<RespuestaCobro> CobrarAsync(decimal monto, string clave, CancellationToken ct = default)
    {
        ValidacionCobro.Exigir(monto, clave); ct.ThrowIfCancellationRequested(); bitacora.Registrar("PASARELA", clave, $"ALTERNATIVO: {monto:F2} USD");
        return Task.FromResult(demo.FallarCobro ? new(false, "", "ALTERNATIVO", "Rechazo simulado.") : diario.Aprobar(monto, clave, "ALTERNATIVO"));
    }
}
public sealed class SelectorPasarela(PasarelaLegadaAdapter legado, PasarelaAlternativa alternativa, IDemostracion demo) : IPasarelaPago
{
    public Task<RespuestaCobro> CobrarAsync(decimal monto, string clave, CancellationToken ct = default)
        => (demo.Proveedor == "LEGADO" ? (IPasarelaPago)legado : alternativa).CobrarAsync(monto, clave, ct);
}
public sealed class AuditoriaPagoDecorator(IPasarelaPago siguiente, IBitacoraTecnica bitacora) : IPasarelaPago
{
    public async Task<RespuestaCobro> CobrarAsync(decimal monto, string clave, CancellationToken ct = default)
    {
        bitacora.Registrar("PAGO_INICIO", clave, $"Importe={monto:F2}");
        try
        {
            var r = await siguiente.CobrarAsync(monto, clave, ct);
            bitacora.Registrar(r.Aprobado ? "PAGO_FIN" : "PAGO_ERROR", clave, $"{r.Proveedor}; aprobado={r.Aprobado}; {r.Mensaje}"); return r;
        }
        catch (Exception e) { bitacora.Registrar("PAGO_ERROR", clave, e.Message); throw; }
    }
}
