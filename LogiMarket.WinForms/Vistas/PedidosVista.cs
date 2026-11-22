using LogiMarket.Application.Consultas;
using LogiMarket.Application.Contratos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Presentacion;
using LogiMarket.Application.Seguridad;

namespace LogiMarket.WinForms.Vistas;

public sealed class PedidosVista : VistaBase
{
    public override string Nombre => "Pedidos e historial";
    private readonly DataGridView _tabla = Ui.Tabla("Pedidos"), _historial = Ui.Tabla("Historial de estados");
    private readonly TextBox _filtro = Ui.Campo("Expresion de filtro", 440); private readonly ConsultasApp _consultas;
    public PedidosVista(FormularioPrincipalMediator mediador, ConsultasApp consultas, PedidosServicio pedidos, InvocadorComandos invocador, SesionUsuario sesion)
        : base(mediador, "Pedidos e historial", "Filtros: ESTADO = PAGADO AND TOTAL >= 50 | CLIENTE = 'Ana' | AND / OR / parentesis.")
    {
        _consultas = consultas; var dividir = new SplitContainer { Size = new(1100, 600), Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 320 }; dividir.Panel1.Controls.Add(_tabla); dividir.Panel2.Controls.Add(_historial);
        var validar = Ui.Boton("Validar seleccionado", () => Operacion("Pedido VALIDADO.", () => pedidos.ValidarAsync(Ui.IdSeleccionado(_tabla))));
        var clonar = Ui.Boton("Clonar borrador", () => Operacion("Copia profunda registrada con nueva identidad.", async () => { await pedidos.ClonarAsync(Ui.IdSeleccionado(_tabla)); }));
        var cancelar = Ui.Boton("Cancelar pedido", async () => { if (Ui.Confirmar(this, "¿Cancelar el pedido seleccionado? Solo se permite desde BORRADOR o VALIDADO.")) await Operacion("Pedido CANCELADO.", async () => { await invocador.EjecutarAsync(new CancelarPedidoCommand(pedidos, Ui.IdSeleccionado(_tabla))); }); });
        foreach (var b in new[] { validar, clonar, cancelar }) b.Enabled = sesion.Exigir().Rol is Rol.ADMIN or Rol.VENTAS;
        Contenido.Controls.Add(dividir); Contenido.Controls.Add(Ui.Fila(validar, clonar, cancelar));
        Contenido.Controls.Add(Ui.Fila(Ui.Texto("Filtro"), _filtro, Ui.Boton("Filtrar", Filtrar), Ui.Boton("Ver todos", async () => { _filtro.Clear(); await Filtrar(); })));
        _tabla.SelectionChanged += (_, _) => Historial();
    }
    protected override void Mostrar(EstadoConsultas d)
    { try { var ast = string.IsNullOrWhiteSpace(_filtro.Text) ? null : new FiltroPedidosParser().Interpretar(_filtro.Text); Cargar(d.Pedidos.Where(p => ast?.Evaluar(p) ?? true)); } catch (FormatException e) { Estado.Text = e.Message; } }
    private void Cargar(IEnumerable<LogiMarket.Domain.Ventas.Pedido> pedidos)
    { Ui.Cargar(_tabla, pedidos.Select(p => new { p.Id, Pedido = p.Id.ToString("N")[..8], Cliente = p.ClienteNombre, p.Estado, Total = p.Valores.Total, Entrega = p.Entrega.Tipo, p.Devuelto, Fecha = p.Creado.ToLocalTime() })); Historial(); }
    private Task Filtrar() => Operacion("Consulta aplicada.", async () => Cargar(await _consultas.FiltrarPedidosAsync(_filtro.Text)), false);
    private void Historial()
    {
        if (Datos is null || _tabla.CurrentRow is null) return; var id = Ui.IdSeleccionado(_tabla); var p = Datos.Pedidos.Single(x => x.Id == id);
        Ui.Cargar(_historial, p.Historial.Select(h => new { h.Desde, h.Hasta, Fecha = h.Fecha.ToLocalTime(), h.Actor }));
    }
}
