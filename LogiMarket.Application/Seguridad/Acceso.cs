using LogiMarket.Application.Contratos;

namespace LogiMarket.Application.Seguridad;

public sealed class SesionUsuario
{
    public Identidad? Actual { get; private set; }
    public Identidad Exigir() => Actual ?? throw new UnauthorizedAccessException("Inicie sesion.");
    public void Iniciar(Identidad identidad) => Actual = identidad;
    public void Cerrar() => Actual = null;
}
public static class Permisos
{
    public static void Exigir(SesionUsuario sesion, params Rol[] permitidos)
    {
        var identidad = sesion.Exigir();
        if (identidad.Rol != Rol.ADMIN && !permitidos.Contains(identidad.Rol))
            throw new UnauthorizedAccessException($"El rol {identidad.Rol} no tiene permiso para esta operacion.");
    }
}
public sealed class Autenticacion(IUsuarios usuarios, IHashContrasena hash, SesionUsuario sesion)
{
    public async Task<Identidad> IngresarAsync(string nombre, string clave)
    {
        var usuario = await usuarios.BuscarAsync(nombre.Trim().ToLowerInvariant());
        if (usuario is null || !hash.Verificar(clave, usuario.Hash)) throw new UnauthorizedAccessException("Usuario o contrasena incorrectos.");
        var identidad = new Identidad(usuario.Id, usuario.Nombre, usuario.Rol); sesion.Iniciar(identidad); return identidad;
    }
    public void CerrarSesion() => sesion.Cerrar();
}
public sealed class PagoAutorizadoProxy(IPasarelaPago siguiente, SesionUsuario sesion) : IPasarelaPago
{
    public Task<RespuestaCobro> CobrarAsync(decimal monto, string clave, CancellationToken ct = default)
    { Permisos.Exigir(sesion, Rol.VENTAS); return siguiente.CobrarAsync(monto, clave, ct); }
}
