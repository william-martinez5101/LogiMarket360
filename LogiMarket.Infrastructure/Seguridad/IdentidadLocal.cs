using System.Security.Cryptography;
using LogiMarket.Application.Contratos;
using LogiMarket.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Seguridad;

public sealed class HashContrasenaPbkdf2 : IHashContrasena
{
    private const int Iteraciones = 210000;
    public string Crear(string clave)
    {
        if (string.IsNullOrWhiteSpace(clave)) throw new ArgumentException("Contrasena obligatoria.");
        var sal = RandomNumberGenerator.GetBytes(16);
        var derivado = Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2-SHA256${Iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(derivado)}";
    }
    public bool Verificar(string clave, string hash)
    {
        try
        {
            var partes = hash.Split('$');
            if (partes.Length != 4 || partes[0] != "PBKDF2-SHA256") return false;
            var iteraciones = int.Parse(partes[1]); if (iteraciones < 100000 || iteraciones > 1000000) return false;
            var derivado = Rfc2898DeriveBytes.Pbkdf2(clave, Convert.FromBase64String(partes[2]), iteraciones, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(derivado, Convert.FromBase64String(partes[3]));
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or OverflowException) { return false; }
    }
}
public sealed class UsuariosEf(ConfiguracionBase configuracion) : IUsuarios
{
    public async Task<UsuarioAutenticable?> BuscarAsync(string nombre)
    {
        await using var db = configuracion.CrearContexto();
        var u = await db.Set<UsuarioFila>().SingleOrDefaultAsync(x => x.Nombre == nombre);
        return u is null ? null : new(u.Id, u.Nombre, u.Rol, u.Hash);
    }
}
