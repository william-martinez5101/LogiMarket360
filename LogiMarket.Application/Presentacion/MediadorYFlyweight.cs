using System.Collections.Concurrent;
using LogiMarket.Application.Consultas;

namespace LogiMarket.Application.Presentacion;

public interface IVistaOperativa { string Nombre { get; } void Actualizar(EstadoConsultas estado); }
public sealed class FormularioPrincipalMediator(ConsultasApp consultas)
{
    private readonly List<IVistaOperativa> _vistas = [];
    public void Registrar(IVistaOperativa vista) { if (!_vistas.Contains(vista)) _vistas.Add(vista); }
    public void Retirar(IVistaOperativa vista) => _vistas.Remove(vista);
    public async Task RefrescarAsync()
    { var estado = await consultas.CargarAsync(); foreach (var vista in _vistas.ToArray()) vista.Actualizar(estado); }
}
public sealed record MetadatosCatalogo(string Sku, string Nombre, string Categoria, string Descripcion);
public sealed record FilaCatalogoVisual(MetadatosCatalogo Metadatos, int Stock, int Cantidad, decimal Precio);
public sealed class CatalogoVisualFlyweightFactory
{
    private readonly ConcurrentDictionary<(string, string, string, string), MetadatosCatalogo> _cache = new();
    public MetadatosCatalogo Obtener(string sku, string nombre, string categoria, string descripcion)
        => _cache.GetOrAdd((sku, nombre, categoria, descripcion), k => new(k.Item1, k.Item2, k.Item3, k.Item4));
    public int Instancias => _cache.Count;
    public IReadOnlyList<FilaCatalogoVisual> CrearFilas(IReadOnlyList<MetadatosCatalogo> catalogo, int filas)
    {
        if (catalogo.Count == 0 || filas <= 0) throw new ArgumentException("Catalogo y numero de filas obligatorios.");
        return Enumerable.Range(0, filas).Select(i => { var m = catalogo[i % catalogo.Count]; return new FilaCatalogoVisual(Obtener(m.Sku, m.Nombre, m.Categoria, m.Descripcion), i % 20, i % 4 + 1, 10m + i % 5); }).ToList();
    }
}
public sealed record ResultadoFlyweight(int Filas, int ObjetosCompartidos, int ObjetosSinCompartir, bool IdentidadCompartida, long BytesCompartidos, long BytesSinCompartir);
public static class MedicionFlyweight
{
    public static ResultadoFlyweight Ejecutar(IReadOnlyList<MetadatosCatalogo> catalogo, int filas = 1000)
    {
        // Calentamiento igual para ambos recorridos. Mide asignaciones del hilo, no memoria residente ni porcentajes.
        CrearSinCompartir(catalogo, 1); new CatalogoVisualFlyweightFactory().CrearFilas(catalogo, 1);
        var inicio = GC.GetAllocatedBytesForCurrentThread(); var fabrica = new CatalogoVisualFlyweightFactory();
        var compartidas = fabrica.CrearFilas(catalogo, filas); var bytesCompartidos = GC.GetAllocatedBytesForCurrentThread() - inicio;
        inicio = GC.GetAllocatedBytesForCurrentThread(); var normales = CrearSinCompartir(catalogo, filas);
        var bytesNormales = GC.GetAllocatedBytesForCurrentThread() - inicio;
        var identidad = filas > catalogo.Count && ReferenceEquals(compartidas[0].Metadatos, compartidas[catalogo.Count].Metadatos);
        GC.KeepAlive(compartidas); GC.KeepAlive(normales);
        return new(filas, fabrica.Instancias, normales.Count, identidad, bytesCompartidos, bytesNormales);
    }
    private static List<FilaCatalogoVisual> CrearSinCompartir(IReadOnlyList<MetadatosCatalogo> catalogo, int filas)
        => Enumerable.Range(0, filas).Select(i => { var m = catalogo[i % catalogo.Count]; return new FilaCatalogoVisual(
            new(m.Sku, m.Nombre, m.Categoria, m.Descripcion), i % 20, i % 4 + 1, 10m + i % 5); }).ToList();
}
