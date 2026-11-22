using System.Runtime.CompilerServices;
using System.Text.Json;
using LogiMarket.Application.Contratos;
using LogiMarket.Application.Consultas;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Gestion;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Ventas;
using LogiMarket.Infrastructure;
using LogiMarket.Infrastructure.Eventos;
using LogiMarket.Infrastructure.Pagos;
using LogiMarket.Infrastructure.Persistencia;
using LogiMarket.Infrastructure.Seguridad;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace LogiMarket.Tests.Integracion;

public sealed class RelojPrueba : IReloj { public DateTimeOffset Ahora { get; set; } = new(2026, 11, 21, 17, 0, 0, TimeSpan.Zero); }
public sealed class Escenario : IAsyncDisposable
{
    public ConfiguracionBase Config { get; }
    public OpcionesDemostracion Demo { get; } = new();
    public RelojPrueba Reloj { get; } = new();
    public SesionUsuario Sesion { get; } = new();
    public HashContrasenaPbkdf2 Hash { get; } = new();
    public IBitacoraTecnica Log { get; }
    public IUnidadTrabajoFactory Unidades { get; }
    public MetricasSesion Metricas { get; } = new();
    public CalculadoraPedido Calculadora { get; } = new([new Mayorista15(), new Vip10(), new SinDescuento()]);
    public FabricaEntrega[] Entregas { get; } = [new EntregaEstandarFactory(), new EntregaExpresFactory()];
    public ConsultasApp Consultas { get; }
    public PedidosServicio Pedidos { get; }
    public DevolucionesServicio Devoluciones { get; }
    public CatalogoServicio Catalogo { get; }
    public ClientesServicio Clientes { get; }
    public ConfirmacionPedidoFacade Facade { get; }
    public InvocadorComandos Invocador { get; }
    public IPasarelaPago Pasarela { get; }
    public DiarioCobrosSimulados Diario { get; }
    public ProcesadorOutbox Outbox { get; }
    public Autenticacion Acceso { get; }
    public CreadorNotificacion[] Creadores { get; }
    public Escenario(string? ruta = null)
    {
        Config = new(ruta ?? Path.Combine(Path.GetTempPath(), "LogiMarket360Tests", Guid.NewGuid().ToString("N"), "prueba.db"));
        Log = new BitacoraArchivo(Config, Reloj); Unidades = new UnidadTrabajoEfFactory(Config, Demo); Diario = new(Config);
        Creadores = [new CreadorEmail(Demo, Reloj, Log), new CreadorSms(Demo, Reloj, Log)];
        var selector = new SelectorPasarela(new(new(Diario, Demo, Log)), new(Diario, Demo, Log), Demo);
        Pasarela = new PagoAutorizadoProxy(new AuditoriaPagoDecorator(selector, Log), Sesion);
        Consultas = new(Unidades, Sesion, Metricas); Pedidos = new(Unidades, Sesion, Reloj, Calculadora, Entregas, Demo);
        Devoluciones = new(Unidades, Sesion, Reloj, [new DevolucionDefecto(), new DevolucionArrepentimiento()], Demo);
        Catalogo = new(Unidades, Sesion, Reloj); Clientes = new(Unidades, Sesion);
        Facade = new(Unidades, Sesion, new(), Pasarela, Reloj, Demo); Invocador = new(Unidades, Reloj, Sesion, Log);
        Outbox = new(Unidades, new([new AuditoriaObserver(Reloj), new MetricasObserver(), new NotificacionObserver(Creadores)]), Metricas, Reloj, Log);
        Acceso = new(new UsuariosEf(Config), Hash, Sesion);
    }
    public static async Task<Escenario> CrearAsync()
    { var e = new Escenario(); await e.InicializarAsync(); return e; }
    public async Task InicializarAsync() { await new InicializadorBase(Config, Hash).MigrarYSeedAsync(); await Acceso.IngresarAsync("admin.demo", "Demo360!"); }
    public Task<Identidad> ComoAsync(Rol rol) => Acceso.IngresarAsync(rol.ToString().ToLowerInvariant() + ".demo", "Demo360!");
    public async Task<Guid> BorradorAsync(string cliente = "Ana", TipoEntrega entrega = TipoEntrega.ESTANDAR, params (string Codigo, int Cantidad)[] lineas)
    {
        if (lineas.Length == 0) lineas = [("KIT_TELETRABAJO", 1)];
        var datos = await Consultas.CargarAsync(); var b = new PedidoBorrador();
        b.SeleccionarCliente(datos.Clientes.Single(x => x.Nombre == cliente).Id); b.SeleccionarEntrega(entrega);
        foreach (var (codigo, cantidad) in lineas)
        {
            var elemento = datos.Productos.Cast<LogiMarket.Domain.Catalogo.IElementoCatalogo>().Concat(datos.Kits).Single(x => x.Codigo == codigo);
            b.Agregar(new(NodoInstantanea.Capturar(elemento), cantidad));
        }
        return await Pedidos.RegistrarAsync(b);
    }
    public async Task<Guid> ValidadoAsync(string cliente = "Ana", TipoEntrega entrega = TipoEntrega.ESTANDAR, params (string Codigo, int Cantidad)[] lineas)
    { var id = await BorradorAsync(cliente, entrega, lineas); await Pedidos.ValidarAsync(id); return id; }
    public async Task<Guid> PagadoAsync(string cliente = "Ana", TipoEntrega entrega = TipoEntrega.ESTANDAR, params (string Codigo, int Cantidad)[] lineas)
    { var id = await ValidadoAsync(cliente, entrega, lineas); await Invocador.EjecutarAsync(new ConfirmarPedidoCommand(Facade, id, "pago-" + id)); return id; }
    public async Task<Guid> EntregadoAsync()
    { var id = await PagadoAsync(); await Pedidos.DespacharAsync(id); await Pedidos.EntregarAsync(id); return id; }
    public async Task EvidenciaAsync(string id, string esperado, object obtenido, [CallerMemberName] string metodo = "")
    {
        var raiz = Environment.GetEnvironmentVariable("LOGIMARKET_EVIDENCE") ?? Path.Combine(AppContext.BaseDirectory, "evidencia");
        Directory.CreateDirectory(raiz);
        var datos = await Consultas.CargarAsync();
        var evidencia = new { Caso = id, Estado = "APROBADO", FechaUtc = DateTimeOffset.UtcNow, Metodo = metodo,
            Fuente = "LogiMarket.Tests/Integracion/AceptacionTests.cs", Esperado = esperado, Obtenido = obtenido,
            Stock = datos.Productos.Select(p => new { p.Sku, p.Stock }), Pagos = datos.Pagos, Movimientos = datos.Movimientos,
            Eventos = datos.Outbox, Avisos = datos.Avisos, Auditoria = datos.Auditoria, Traza = Log.Leer(), CobrosSimulados = Diario.Leer() };
        await File.WriteAllTextAsync(Path.Combine(raiz, id + ".json"), JsonSerializer.Serialize(evidencia, new JsonSerializerOptions { WriteIndented = true }));
    }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
