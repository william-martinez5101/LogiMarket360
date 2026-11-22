using System.Text;
using LogiMarket.Application.Contratos;

namespace LogiMarket.Infrastructure.Documentos;

public sealed class ArchivoTxt(string directorio) : ISalidaDocumento
{
    public async Task<DocumentoEmitido> EmitirAsync(string nombre, string contenido)
    {
        Directory.CreateDirectory(directorio);
        var ruta = Path.Combine(directorio, nombre + ".txt"); await File.WriteAllTextAsync(ruta, contenido, new UTF8Encoding(false));
        return new(contenido, ruta);
    }
}
