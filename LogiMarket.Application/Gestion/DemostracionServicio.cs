using LogiMarket.Application.Contratos;
using LogiMarket.Application.Seguridad;
namespace LogiMarket.Application.Gestion;
public sealed class DemostracionServicio(IDemostracion opciones, SesionUsuario sesion)
{
    public void SeleccionarProveedor(string proveedor)
    { Permisos.Exigir(sesion, Rol.VENTAS); ValidarProveedor(proveedor); opciones.Proveedor = proveedor; }
    public void Configurar(string proveedor, string canal, bool falloCobro, bool falloNotificador, bool falloPersistencia = false)
    {
        Permisos.Exigir(sesion); ValidarProveedor(proveedor);
        if (canal is not ("EMAIL" or "SMS")) throw new ArgumentException("Canal desconocido.");
        opciones.Proveedor = proveedor; opciones.Canal = canal; opciones.FallarCobro = falloCobro; opciones.FallarNotificacion = falloNotificador;
        opciones.FallarPersistencia = falloPersistencia;
    }
    private static void ValidarProveedor(string proveedor)
    { if (proveedor is not ("LEGADO" or "ALTERNATIVO")) throw new ArgumentException("Proveedor desconocido."); }
}
