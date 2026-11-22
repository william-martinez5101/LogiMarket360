using System.Text.Json;
using LogiMarket.Application.Consultas;
using LogiMarket.Application.Contratos;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Gestion;
using LogiMarket.Application.Presentacion;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Catalogo;

namespace LogiMarket.WinForms.Vistas;

public sealed class AuditoriaVista : VistaBase
{
    public override string Nombre => "Auditoria / pruebas";
    private readonly DataGridView _auditoria = Ui.Tabla("Auditoria de negocio y comandos"), _outbox = Ui.Tabla("Eventos Outbox"), _avisos = Ui.Tabla("Notificaciones locales"), _trazas = Ui.Tabla("Trazas de colaboradores"), _flyweight = Ui.Tabla("Mil filas Flyweight");
    private readonly ComboBox _proveedor = Ui.Combo("Proveedor demostracion", 160), _canal = Ui.Combo("Canal de aviso", 110);
    private readonly CheckBox _falloCobro = new() { Text = "Rechazar cobro", AutoSize = true, Margin = new(8) }, _falloAviso = new() { Text = "Fallar notificador", AutoSize = true, Margin = new(8) };
    private readonly CheckBox _falloPersistencia = new() { Text = "Fallar guardado del cobro", AccessibleName = "Fallar guardado del cobro tras aceptacion", AutoSize = true, Margin = new(8) };
    private readonly Label _metricas = Ui.Texto("", 11, true); private readonly MetricasSesion _sesion; private readonly IBitacoraTecnica _tecnica;
    private readonly CatalogoVisualFlyweightFactory _fabrica;
    private IReadOnlyList<FilaCatalogoVisual> _filasFlyweight = [];
    private int _paginaFlyweight;
    private readonly Label _pagina = Ui.Texto("Genere el conjunto de 1000 filas.");
    public AuditoriaVista(FormularioPrincipalMediator mediador, MetricasSesion metricas, IBitacoraTecnica tecnica, IDemostracion demo,
        DemostracionServicio demostracion, ProcesadorOutbox procesador, CatalogoVisualFlyweightFactory fabrica, SesionUsuario sesion)
        : base(mediador, "Auditoria, metricas y demostracion", "Escenarios locales controlados. Ningun correo, SMS ni cobro se envia a servicios externos.")
    {
        _sesion = metricas; _tecnica = tecnica; _fabrica = fabrica; _proveedor.DataSource = new[] { "LEGADO", "ALTERNATIVO" }; _proveedor.SelectedItem = demo.Proveedor;
        _canal.DataSource = new[] { "EMAIL", "SMS" }; _canal.SelectedItem = demo.Canal;
        var aplicar = Ui.Boton("Aplicar escenario", () => Operacion("Escenario aplicado. Los fallos afectan las proximas operaciones reales de demostracion.", () => {
            demostracion.Configurar((string)_proveedor.SelectedItem!, (string)_canal.SelectedItem!, _falloCobro.Checked, _falloAviso.Checked, _falloPersistencia.Checked); return Task.CompletedTask;
        }, false));
        var reprocesar = Ui.Boton("Reprocesar Outbox", () => Operacion("Outbox reprocesado. Revise pendientes e intentos.", async () => {
            Permisos.Exigir(sesion); await procesador.ProcesarAsync();
        }), true);
        aplicar.Enabled = reprocesar.Enabled = _falloPersistencia.Enabled = _falloAviso.Enabled = _falloCobro.Enabled = _proveedor.Enabled = _canal.Enabled = sesion.Exigir().Rol == Rol.ADMIN;
        var tabs = new TabControl { Dock = DockStyle.Fill, AccessibleName = "Registros de auditoria" };
        foreach (var (titulo, control) in new[] { ("Auditoria", _auditoria), ("Outbox", _outbox), ("Notificaciones", _avisos), ("Trazas", _trazas), ("Flyweight 1000", _flyweight) })
        {
            var pagina = new TabPage(titulo); pagina.Controls.Add(control); tabs.TabPages.Add(pagina);
            if (control == _flyweight) pagina.Controls.Add(Ui.Fila(
                Ui.Boton("Anterior 50", () => { if (_paginaFlyweight > 0) _paginaFlyweight--; MostrarPagina(); return Task.CompletedTask; }),
                Ui.Boton("Siguiente 50", () => { if ((_paginaFlyweight + 1) * 50 < _filasFlyweight.Count) _paginaFlyweight++; MostrarPagina(); return Task.CompletedTask; }), _pagina));
        }
        var pie = Ui.Fila(_metricas); pie.Dock = DockStyle.Bottom;
        Contenido.Controls.Add(tabs); Contenido.Controls.Add(pie);
        Contenido.Controls.Add(Ui.Fila(Ui.Boton("Demostrar Flyweight", Flyweight), Ui.Boton("Recorrer kit (Iterator/Visitor)", RecorrerKit)));
        Contenido.Controls.Add(Ui.Fila(_proveedor, _canal, _falloCobro, _falloAviso, _falloPersistencia, aplicar, reprocesar));
    }
    protected override void Mostrar(EstadoConsultas d)
    {
        Ui.Cargar(_auditoria, d.Auditoria.Select(a => new { a.Id, a.Tipo, a.Correlacion, a.Mensaje, Fecha = a.Fecha.ToLocalTime() }));
        Ui.Cargar(_outbox, d.Outbox.Select(e => new { Evento = e.Evento.Id.ToString("N")[..8], Pedido = e.Evento.PedidoId.ToString("N")[..8], e.Evento.Tipo, e.Evento.Canal, e.Intentos, Estado = e.Procesado is null ? "PENDIENTE" : "PROCESADO", e.Error }));
        Ui.Cargar(_avisos, d.Avisos.Select(a => new { a.EventoId, a.Canal, a.Mensaje, Fecha = a.Fecha.ToLocalTime() }));
        Ui.Cargar(_trazas, _tecnica.Leer().OrderByDescending(x => x.Fecha).Select(a => new { a.Id, a.Tipo, a.Correlacion, a.Mensaje, Fecha = a.Fecha.ToLocalTime() }));
        _metricas.Text = $"Metricas compartidas por DI | Pagos {_sesion.Pagos} | Despachos {_sesion.Despachos} | Devoluciones {_sesion.Devoluciones} | Ventas USD {_sesion.Ventas:N2} | Reembolsos USD {_sesion.Reembolsos:N2}";
    }
    private Task Flyweight() => Operacion("Medicion y 1000 filas generadas. Consulte la pestana Flyweight 1000.", () => {
        var catalogo = Datos!.Productos.Select(p => new MetadatosCatalogo(p.Sku, p.Nombre, "Accesorios", "Accesorio de computacion")).ToList();
        var resultado = MedicionFlyweight.Ejecutar(catalogo);
        _filasFlyweight = _fabrica.CrearFilas(catalogo, 1000); _paginaFlyweight = 0; MostrarPagina();
        _metricas.Text = $"1000 filas | Metadatos {resultado.ObjetosCompartidos} vs {resultado.ObjetosSinCompartir} | ReferenceEquals={resultado.IdentidadCompartida} | Bytes asignados: {resultado.BytesCompartidos} vs {resultado.BytesSinCompartir}";
        _tecnica.Registrar("FLYWEIGHT", Guid.NewGuid().ToString(), JsonSerializer.Serialize(resultado)); return Task.CompletedTask;
    }, false);
    private void MostrarPagina()
    {
        var inicio = _paginaFlyweight * 50;
        Ui.Cargar(_flyweight, _filasFlyweight.Skip(inicio).Take(50).Select((f, i) => new { Fila = inicio + i + 1, f.Metadatos.Sku, f.Metadatos.Nombre, f.Metadatos.Categoria, f.Stock, f.Cantidad, f.Precio }));
        _pagina.Text = _filasFlyweight.Count == 0 ? "Genere el conjunto de 1000 filas." : $"Filas {inicio + 1}-{Math.Min(inicio + 50, _filasFlyweight.Count)} de {_filasFlyweight.Count}; metadatos compartidos entre todas las paginas.";
    }
    private Task RecorrerKit() => Operacion("Recorrido real del kit completado.", () => {
        var kit = Datos!.Kits.Single(x => x.Codigo == "KIT_TELETRABAJO"); using var iterador = new IteradorFisico(kit); var hojas = new List<string>();
        while (iterador.MoveNext()) hojas.Add(iterador.Current.Producto.Sku + " x" + iterador.Current.Cantidad);
        var peso = new PesoLogisticoVisitor(); kit.Aceptar(peso); _metricas.Text = $"Iterator: {string.Join(", ", hojas)} | Visitor peso {peso.PesoKg:N2} kg | Composite precio USD {kit.CalcularPrecio():N2}";
        _tecnica.Registrar("KIT_RECORRIDO", kit.Codigo, _metricas.Text); return Task.CompletedTask;
    }, false);
}
