using LogiMarket.Application.Consultas;
using LogiMarket.Application.Contratos;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Gestion;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Presentacion;
using LogiMarket.Application.Seguridad;

namespace LogiMarket.WinForms.Vistas;

public sealed class CobroVista : VistaBase
{
    public override string Nombre => "Cobro";
    private readonly DataGridView _tabla = Ui.Tabla("Pedidos para cobro"); private readonly ComboBox _proveedor = Ui.Combo("Proveedor simulado", 170);
    private readonly TextBox _clave = Ui.Campo("Clave de idempotencia", 370); private readonly Label _resumen = Ui.Texto("", 12, true), _resultado = Ui.Texto(""); private Guid? _seleccionado;
    public CobroVista(FormularioPrincipalMediator mediador, ConfirmacionPedidoFacade facade, InvocadorComandos invocador,
        ProcesadorOutbox outbox, IDemostracion demo, DemostracionServicio demostracion, SesionUsuario sesion)
        : base(mediador, "Confirmacion y cobro", "Cobro local simulado. La misma clave recupera el resultado original sin duplicar inventario.")
    {
        _proveedor.DataSource = new[] { "LEGADO", "ALTERNATIVO" }; _proveedor.SelectedItem = demo.Proveedor;
        var confirmar = Ui.Boton("Confirmar cobro", async () => {
            if (_tabla.CurrentRow is null) throw new InvalidOperationException("Seleccione un pedido.");
            if (!Ui.Confirmar(this, "¿Confirmar el cobro simulado y descontar el stock del pedido seleccionado?")) return;
            await Operacion("Cobro confirmado. Catalogo, pedidos y metricas actualizados.", async () => {
                demostracion.SeleccionarProveedor((string)_proveedor.SelectedItem!);
                var pago = await invocador.EjecutarAsync(new ConfirmarPedidoCommand(facade, Ui.IdSeleccionado(_tabla), _clave.Text.Trim()));
                var r = await outbox.ProcesarAsync(); _resultado.Text = $"USD {pago.Importe:N2} | {pago.Proveedor} | {pago.Referencia}\nOutbox pendientes: {r.Pendientes}";
            });
        }, true);
        confirmar.Enabled = _proveedor.Enabled = sesion.Exigir().Rol is Rol.ADMIN or Rol.VENTAS;
        var pie = Ui.Fila(_resumen, _resultado); pie.Dock = DockStyle.Bottom;
        Contenido.Controls.Add(_tabla); Contenido.Controls.Add(pie);
        Contenido.Controls.Add(Ui.Fila(Ui.Texto("Proveedor"), _proveedor, confirmar));
        Contenido.Controls.Add(Ui.Fila(Ui.Texto("Idempotencia"), _clave)); _tabla.SelectionChanged += (_, _) => Resumen();
    }
    protected override void Mostrar(EstadoConsultas d)
    { Ui.Cargar(_tabla, d.Pedidos.Select(p => new { p.Id, Pedido = p.Id.ToString("N")[..8], Cliente = p.ClienteNombre, p.Estado, Subtotal = p.Valores.Subtotal, Descuento = p.Valores.Descuento, Tarifa = p.Valores.Tarifa, Total = p.Valores.Total })); Resumen(); }
    private void Resumen()
    {
        if (_tabla.CurrentRow is null || Datos is null) return; var id = Ui.IdSeleccionado(_tabla); var p = Datos.Pedidos.Single(x => x.Id == id);
        if (_seleccionado != id) { _clave.Text = "pago-" + id.ToString("N"); _seleccionado = id; }
        _resumen.Text = $"{p.ClienteNombre} | {p.Estado}\nSubtotal {p.Valores.Subtotal:N2} - descuento {p.Valores.Descuento:N2} + tarifa {p.Valores.Tarifa:N2} = USD {p.Valores.Total:N2}";
    }
}

public sealed class DespachoVista : VistaBase
{
    public override string Nombre => "Despacho y entrega";
    private readonly DataGridView _tabla = Ui.Tabla("Pedidos logisticos"), _guias = Ui.Tabla("Guias registradas");
    public DespachoVista(FormularioPrincipalMediator mediador, PedidosServicio pedidos, InvocadorComandos invocador, ProcesadorOutbox outbox, SesionUsuario sesion)
        : base(mediador, "Despacho y entrega", "Guia simulada, plazo estimado y transiciones protegidas para BODEGA / ADMIN.")
    {
        var dividir = new SplitContainer { Size = new(1100, 600), Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 320 }; dividir.Panel1.Controls.Add(_tabla); dividir.Panel2.Controls.Add(_guias);
        var despachar = Ui.Boton("Registrar despacho", async () => { if (Ui.Confirmar(this, "¿Despachar el pedido seleccionado? Debe estar PAGADO.")) await Operacion("Despacho registrado y evento Outbox procesado.", async () => { await invocador.EjecutarAsync(new DespacharPedidoCommand(pedidos, Ui.IdSeleccionado(_tabla))); await outbox.ProcesarAsync(); }); }, true);
        var entregar = Ui.Boton("Registrar entrega", async () => { if (Ui.Confirmar(this, "¿Registrar la entrega del pedido seleccionado? Debe estar DESPACHADO.")) await Operacion("Entrega registrada.", () => pedidos.EntregarAsync(Ui.IdSeleccionado(_tabla))); });
        despachar.Enabled = entregar.Enabled = sesion.Exigir().Rol is Rol.ADMIN or Rol.BODEGA;
        Contenido.Controls.Add(dividir); Contenido.Controls.Add(Ui.Fila(despachar, entregar));
    }
    protected override void Mostrar(EstadoConsultas d)
    {
        Ui.Cargar(_tabla, d.Pedidos.Select(p => new { p.Id, Pedido = p.Id.ToString("N")[..8], Cliente = p.ClienteNombre, p.Estado, Entrega = p.Entrega.Tipo, PlazoDias = p.Entrega.Dias, Total = p.Valores.Total }));
        Ui.Cargar(_guias, d.Despachos.Select(g => new { g.PedidoId, Pedido = g.PedidoId.ToString("N")[..8], g.Guia, Fecha = g.Fecha.ToLocalTime(), Estimada = g.Estimada.ToLocalTime() }));
    }
}

public sealed class DevolucionesVista : VistaBase
{
    public override string Nombre => "Devoluciones";
    private readonly DataGridView _tabla = Ui.Tabla("Pedidos y devoluciones"); private readonly ComboBox _motivo = Ui.Combo("Motivo", 230);
    private readonly CheckBox _sinUso = new() { Text = "Producto sin uso", Checked = true, AutoSize = true, Margin = new(10) }; private readonly Label _detalle = Ui.Texto("", 12, true);
    public DevolucionesVista(FormularioPrincipalMediator mediador, DevolucionesServicio devoluciones, InvocadorComandos invocador, ProcesadorOutbox outbox, SesionUsuario sesion)
        : base(mediador, "Devoluciones y reintegros", "DEFECTO: hasta 30 dias. ARREPENTIMIENTO: hasta 7 dias y sin uso. Reembolso total de productos, sin flete.")
    {
        _motivo.DataSource = new[] { "DEFECTO", "ARREPENTIMIENTO" };
        var confirmar = Ui.Boton("Confirmar devolucion", async () => {
            if (!Ui.Confirmar(this, "¿Procesar la devolucion total del pedido seleccionado? Se reintegrara stock y se registrara un reembolso simulado unico.")) return;
            await Operacion("Devolucion aceptada. Stock reintegrado una vez y reembolso sin tarifa de envio.", async () => {
                var r = await invocador.EjecutarAsync(new ProcesarDevolucionCommand(devoluciones, new(Ui.IdSeleccionado(_tabla), (string)_motivo.SelectedItem!, _sinUso.Checked)));
                await outbox.ProcesarAsync(); _detalle.Text = $"Reembolso USD {r.Reembolso:N2} | {r.Referencia}";
            });
        }, true); confirmar.Enabled = sesion.Exigir().Rol is Rol.ADMIN or Rol.VENTAS;
        var pie = Ui.Fila(_detalle); pie.Dock = DockStyle.Bottom;
        Contenido.Controls.Add(_tabla); Contenido.Controls.Add(pie); Contenido.Controls.Add(Ui.Fila(Ui.Texto("Motivo"), _motivo, _sinUso, confirmar));
        _tabla.SelectionChanged += (_, _) => Detalle();
    }
    protected override void Mostrar(EstadoConsultas d)
    { Ui.Cargar(_tabla, d.Pedidos.Select(p => new { p.Id, Pedido = p.Id.ToString("N")[..8], Cliente = p.ClienteNombre, p.Estado, p.Devuelto, Total = p.Valores.Total, Entregado = p.FechaEntrega?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "Pendiente" })); Detalle(); }
    private void Detalle()
    {
        if (_tabla.CurrentRow is null || Datos is null) return; var id = Ui.IdSeleccionado(_tabla); var dev = Datos.Devoluciones.SingleOrDefault(x => x.PedidoId == id);
        _detalle.Text = dev is null ? "Seleccione un pedido ENTREGADO y un motivo para evaluar su elegibilidad." : $"Reembolso USD {dev.Reembolso:N2} | {dev.Motivo} | {dev.Referencia}";
    }
}
