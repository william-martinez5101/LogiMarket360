namespace LogiMarket.Domain.Ventas;

public enum TipoEntrega { ESTANDAR, EXPRES }
public interface ITarifaEntrega { TipoEntrega Familia { get; } decimal Importe { get; } }
public interface IPlazoEntrega { TipoEntrega Familia { get; } int Dias { get; } }
public interface IEtiquetaEntrega { TipoEntrega Familia { get; } string Crear(Guid pedidoId); }

public abstract class FabricaEntrega
{
    public abstract TipoEntrega Tipo { get; }
    public abstract ITarifaEntrega CrearTarifa();
    public abstract IPlazoEntrega CrearPlazo();
    public abstract IEtiquetaEntrega CrearEtiqueta();
    public PlanEntrega CrearPlan(Guid pedidoId)
    {
        var tarifa = CrearTarifa(); var plazo = CrearPlazo(); var etiqueta = CrearEtiqueta();
        if (tarifa.Familia != Tipo || plazo.Familia != Tipo || etiqueta.Familia != Tipo)
            throw new InvalidOperationException("No se pueden mezclar familias de entrega.");
        return new(Tipo, tarifa.Importe, plazo.Dias, etiqueta.Crear(pedidoId));
    }
}

public sealed record PlanEntrega(TipoEntrega Tipo, decimal Tarifa, int Dias, string Guia);
internal sealed record TarifaEntrega(TipoEntrega Familia, decimal Importe) : ITarifaEntrega;
internal sealed record PlazoEntrega(TipoEntrega Familia, int Dias) : IPlazoEntrega;
internal sealed record EtiquetaEntrega(TipoEntrega Familia) : IEtiquetaEntrega
{
    public string Crear(Guid pedidoId) => $"GUIA-{Familia}-{pedidoId:N}";
}
public sealed class EntregaEstandarFactory : FabricaEntrega
{
    public override TipoEntrega Tipo => TipoEntrega.ESTANDAR;
    public override ITarifaEntrega CrearTarifa() => new TarifaEntrega(Tipo, 5m);
    public override IPlazoEntrega CrearPlazo() => new PlazoEntrega(Tipo, 3);
    public override IEtiquetaEntrega CrearEtiqueta() => new EtiquetaEntrega(Tipo);
}
public sealed class EntregaExpresFactory : FabricaEntrega
{
    public override TipoEntrega Tipo => TipoEntrega.EXPRES;
    public override ITarifaEntrega CrearTarifa() => new TarifaEntrega(Tipo, 12m);
    public override IPlazoEntrega CrearPlazo() => new PlazoEntrega(Tipo, 1);
    public override IEtiquetaEntrega CrearEtiqueta() => new EtiquetaEntrega(Tipo);
}
