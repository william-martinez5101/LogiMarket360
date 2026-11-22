using LogiMarket.Application.Contratos;
using LogiMarket.Application.Consultas;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Gestion;
using LogiMarket.Application.Inventario;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Presentacion;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Ventas;
using LogiMarket.Infrastructure;
using LogiMarket.Infrastructure.Eventos;
using LogiMarket.Infrastructure.Pagos;
using LogiMarket.Infrastructure.Persistencia;
using LogiMarket.Infrastructure.Seguridad;
using LogiMarket.Infrastructure.Documentos;
using LogiMarket.WinForms.Vistas;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var configuracion = ConfiguracionBase.Predeterminada();
        var indice = Array.IndexOf(args, "--db");
        if (indice >= 0 && indice + 1 < args.Length) configuracion = new(Path.GetFullPath(args[indice + 1]));
        try
        {
            using var servicios = Componer(configuracion);
            servicios.GetRequiredService<InicializadorBase>().MigrarYSeedAsync().GetAwaiter().GetResult();
            var recuperacion = servicios.GetRequiredService<ProcesadorOutbox>().ProcesarAsync().GetAwaiter().GetResult();
            if (args.Contains("--initialize") || args.Contains("--seed") || args.Contains("--migrate"))
            {
                File.WriteAllText(Path.Combine(configuracion.Directorio, "inicializacion.log"), $"{DateTimeOffset.UtcNow:O}\nMigraciones y seed aplicados.\nBase: {configuracion.Ruta}\nOutbox: {recuperacion}\n"); return;
            }
            while (true)
            {
                using var alcance = servicios.CreateScope();
                using var acceso = alcance.ServiceProvider.GetRequiredService<AccesoForm>();
                if (acceso.ShowDialog() != DialogResult.OK) break;
                using var principal = alcance.ServiceProvider.GetRequiredService<VentanaPrincipal>();
                System.Windows.Forms.Application.Run(principal);
                servicios.GetRequiredService<Autenticacion>().CerrarSesion();
                if (!principal.VolverAIngresar) break;
            }
        }
        catch (Exception e)
        {
            Directory.CreateDirectory(configuracion.Directorio); File.WriteAllText(Path.Combine(configuracion.Directorio, "error-inicio.log"), e.ToString());
            MessageBox.Show($"No se pudo iniciar LogiMarket360.\n{e.Message}\nConsulte error-inicio.log en {configuracion.Directorio}.", "Error de inicio", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.ExitCode = 1;
        }
    }
    // Composition root: unico archivo de WinForms que conoce implementaciones de Infrastructure.
    private static ServiceProvider Componer(ConfiguracionBase configuracion)
    {
        var s = new ServiceCollection();
        s.AddSingleton(configuracion); s.AddSingleton(new RutasAplicacion(Path.GetFullPath(configuracion.Ruta), Path.Combine(configuracion.Directorio, "comprobantes"), configuracion.Directorio));
        s.AddSingleton<IReloj, RelojSistema>(); s.AddSingleton<IDemostracion, OpcionesDemostracion>();
        s.AddSingleton<IHashContrasena, HashContrasenaPbkdf2>(); s.AddSingleton<IUsuarios, UsuariosEf>();
        s.AddSingleton<SesionUsuario>(); s.AddSingleton<Autenticacion>(); s.AddSingleton<InicializadorBase>();
        s.AddSingleton<IUnidadTrabajoFactory, UnidadTrabajoEfFactory>(); s.AddSingleton<IBitacoraTecnica, BitacoraArchivo>();
        s.AddSingleton<MetricasSesion>(); s.AddSingleton<CatalogoVisualFlyweightFactory>();
        s.AddSingleton<IDescuento, Mayorista15>(); s.AddSingleton<IDescuento, Vip10>(); s.AddSingleton<IDescuento, SinDescuento>(); s.AddSingleton<CalculadoraPedido>();
        s.AddSingleton<FabricaEntrega, EntregaEstandarFactory>(); s.AddSingleton<FabricaEntrega, EntregaExpresFactory>();
        s.AddSingleton<DiarioCobrosSimulados>(); s.AddSingleton<PasarelaLegadaSimulada>(); s.AddSingleton<PasarelaLegadaAdapter>(); s.AddSingleton<PasarelaAlternativa>(); s.AddSingleton<SelectorPasarela>();
        s.AddSingleton<IIntentosCobro>(sp => sp.GetRequiredService<DiarioCobrosSimulados>());
        s.AddSingleton<DisponibilidadExistencias>();
        s.AddSingleton<IPasarelaPago>(sp => new PagoAutorizadoProxy(new AuditoriaPagoDecorator(sp.GetRequiredService<SelectorPasarela>(), sp.GetRequiredService<IBitacoraTecnica>()), sp.GetRequiredService<SesionUsuario>()));
        s.AddSingleton<CadenaConfirmacion>(); s.AddSingleton<ConfirmacionPedidoFacade>(); s.AddSingleton<InvocadorComandos>();
        s.AddSingleton<CreadorNotificacion, CreadorEmail>(); s.AddSingleton<CreadorNotificacion, CreadorSms>();
        s.AddSingleton<IObservadorEvento, AuditoriaObserver>(); s.AddSingleton<IObservadorEvento, MetricasObserver>(); s.AddSingleton<IObservadorEvento, NotificacionObserver>();
        s.AddSingleton<PublicadorEventos>(); s.AddSingleton<ProcesadorOutbox>();
        s.AddSingleton<ProcesarDevolucion, DevolucionDefecto>(); s.AddSingleton<ProcesarDevolucion, DevolucionArrepentimiento>(); s.AddSingleton<DevolucionesServicio>();
        s.AddSingleton<ConsultasApp>(); s.AddSingleton<CatalogoServicio>(); s.AddSingleton<ClientesServicio>(); s.AddSingleton<PedidosServicio>(); s.AddSingleton<ValoradorBorrador>(); s.AddSingleton<DemostracionServicio>();
        s.AddSingleton<ISalidaDocumento>(_ => new ArchivoTxt(Path.Combine(configuracion.Directorio, "comprobantes")));
        s.AddScoped<FormularioPrincipalMediator>();
        s.AddScoped<VistaBase, PanelVista>(); s.AddScoped<VistaBase, CatalogoVista>(); s.AddScoped<VistaBase, ClientesVista>();
        s.AddScoped<VistaBase, CarritoVista>(); s.AddScoped<VistaBase, PedidosVista>(); s.AddScoped<VistaBase, CobroVista>();
        s.AddScoped<VistaBase, DespachoVista>(); s.AddScoped<VistaBase, DevolucionesVista>(); s.AddScoped<VistaBase, InventarioVista>();
        s.AddScoped<VistaBase, ReportesVista>(); s.AddScoped<VistaBase, AuditoriaVista>();
        s.AddScoped<AccesoForm>(); s.AddScoped<VentanaPrincipal>();
        return s.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
