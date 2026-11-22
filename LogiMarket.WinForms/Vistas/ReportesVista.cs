using LogiMarket.Application.Consultas;
using LogiMarket.Application.Contratos;
using LogiMarket.Application.Documentos;
using LogiMarket.Application.Presentacion;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.WinForms.Vistas;

public sealed class VistaPrevia : ISalidaDocumento
{
    private readonly IWin32Window _propietario;
    public VistaPrevia(IWin32Window propietario) => _propietario = propietario;
    public Task<DocumentoEmitido> EmitirAsync(string nombre, string contenido)
    {
        using var ventana = new Form { Text = "Vista previa - " + nombre, Size = new(850, 650), MinimumSize = new(650, 450), StartPosition = FormStartPosition.CenterParent, AutoScaleMode = AutoScaleMode.Dpi };
        ventana.Controls.Add(new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Text = contenido, Font = new("Consolas", 12), BackColor = Color.White, AccessibleName = "Contenido del comprobante" });
        ventana.ShowDialog(_propietario); return Task.FromResult(new DocumentoEmitido(contenido, null));
    }
}
public sealed class ReportesVista : VistaBase
{
    public override string Nombre => "Reportes";
    private readonly DataGridView _tabla = Ui.Tabla("Reporte de pedidos"); private readonly TextBox _filtro = Ui.Campo("Filtro de reporte", 390);
    private readonly DateTimePicker _desde = new() { Format = DateTimePickerFormat.Short, Width = 165, Value = DateTime.Today.AddMonths(-1), ShowCheckBox = true, Checked = false, AccessibleName = "Desde" };
    private readonly DateTimePicker _hasta = new() { Format = DateTimePickerFormat.Short, Width = 165, Value = DateTime.Today, ShowCheckBox = true, Checked = false, AccessibleName = "Hasta" };
    private readonly Label _resumen = Ui.Texto("", 12, true); private readonly ConsultasApp _consultas; private readonly ISalidaDocumento _archivo;
    public ReportesVista(FormularioPrincipalMediator mediador, ConsultasApp consultas, ISalidaDocumento archivo)
        : base(mediador, "Reportes y comprobantes", "Filtros interpretados, fechas opcionales y comprobantes internos por vista previa o archivo TXT.")
    {
        _consultas = consultas; _archivo = archivo;
        var pie = Ui.Fila(_resumen); pie.Dock = DockStyle.Bottom;
        Contenido.Controls.Add(_tabla); Contenido.Controls.Add(pie);
        Contenido.Controls.Add(Ui.Fila(Ui.Boton("Vista previa venta", () => Emitir(false, false)), Ui.Boton("TXT venta", () => Emitir(false, true)), Ui.Boton("Vista previa devolucion", () => Emitir(true, false)), Ui.Boton("TXT devolucion", () => Emitir(true, true))));
        Contenido.Controls.Add(Ui.Fila(Ui.Texto("Expresion"), _filtro, Ui.Boton("Consultar", Filtrar, true)));
        Contenido.Controls.Add(Ui.Fila(Ui.Texto("Desde"), _desde, Ui.Texto("Hasta"), _hasta, Ui.Texto("Marque las fechas para limitar el periodo.")));
    }
    protected override void Mostrar(EstadoConsultas d) => Cargar(d.Pedidos);
    private void Cargar(IEnumerable<Pedido> pedidos)
    {
        var lista = pedidos.ToArray(); var ids = lista.Select(p => p.Id).ToHashSet();
        Ui.Cargar(_tabla, lista.Select(p => new { p.Id, Pedido = p.Id.ToString("N")[..8], Cliente = p.ClienteNombre, p.Estado, Total = p.Valores.Total, p.Devuelto, Fecha = p.Creado.ToLocalTime() }));
        var ventas = Datos?.Pagos.Where(p => ids.Contains(p.PedidoId)).Sum(p => p.Importe) ?? 0;
        var devoluciones = Datos?.Devoluciones.Where(p => ids.Contains(p.PedidoId)).ToArray() ?? [];
        _resumen.Text = $"Pedidos: {lista.Length}   |   Cobros USD {ventas:N2}   |   Devoluciones: {devoluciones.Length}   |   Neto USD {ventas - devoluciones.Sum(d => d.Reembolso):N2}";
    }
    private Task Filtrar() => Operacion("Reporte actualizado.", async () => {
        DateTimeOffset? desde = _desde.Checked ? new DateTimeOffset(_desde.Value.Date) : null;
        DateTimeOffset? hasta = _hasta.Checked ? new DateTimeOffset(_hasta.Value.Date.AddDays(1).AddTicks(-1)) : null;
        if (desde > hasta) throw new ArgumentException("La fecha inicial debe ser anterior a la final.");
        Cargar(await _consultas.FiltrarPedidosAsync(_filtro.Text, desde, hasta));
    }, false);
    private Task Emitir(bool devolucion, bool txt) => Operacion(txt ? "Comprobante TXT generado en la carpeta de comprobantes." : "Vista previa mostrada.", async () => {
        var p = Datos!.Pedidos.Single(x => x.Id == Ui.IdSeleccionado(_tabla)); var salida = txt ? _archivo : new VistaPrevia(FindForm() ?? throw new InvalidOperationException("Vista no alojada en una ventana."));
        Comprobante comprobante = devolucion ? new ComprobanteDevolucion(salida) : new ComprobanteVenta(salida);
        var r = await comprobante.EmitirAsync(p, Datos.Devoluciones.SingleOrDefault(x => x.PedidoId == p.Id));
        if (txt) _resumen.Text = "TXT guardado: " + r.Ruta;
    }, false);
}
