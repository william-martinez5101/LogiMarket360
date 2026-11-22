using System.ComponentModel;
using LogiMarket.Application.Consultas;
using LogiMarket.Application.Presentacion;

namespace LogiMarket.WinForms.Vistas;

internal static class Ui
{
    public static readonly Color Fondo = Color.FromArgb(245, 247, 251);
    public static readonly Color Azul = Color.FromArgb(31, 74, 125);
    public static Label Texto(string texto, float tamano = 10, bool negrita = false) => new() { Text = texto, AutoSize = true, MaximumSize = new(1400, 0), Font = new("Segoe UI", tamano, negrita ? FontStyle.Bold : FontStyle.Regular), Margin = new(5, 8, 10, 6) };
    public static TextBox Campo(string nombre, int ancho = 220) => new() { AccessibleName = nombre, Width = ancho, Margin = new(5, 5, 8, 5) };
    public static NumericUpDown Numero(string nombre, int decimales = 0, decimal minimo = 0, decimal maximo = 1000000) => new() { AccessibleName = nombre, Width = 130, DecimalPlaces = decimales, Minimum = minimo, Maximum = maximo, Margin = new(5), ThousandsSeparator = true };
    public static ComboBox Combo(string nombre, int ancho = 240) => new() { AccessibleName = nombre, Width = ancho, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new(5) };
    public static Button Boton(string texto, Func<Task> accion, bool primario = false)
    {
        var b = new Button { Text = texto, AccessibleName = texto, AutoSize = true, MinimumSize = new(95, 34), Height = 36, Margin = new(5), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, BackColor = primario ? Azul : Color.White, ForeColor = primario ? Color.White : Color.FromArgb(28, 42, 62), Padding = new(9, 3, 9, 3) };
        b.FlatAppearance.BorderColor = Color.FromArgb(199, 209, 223);
        b.Click += async (_, _) => { b.Enabled = false; try { await accion(); } catch (Exception e) { MessageBox.Show(b.FindForm(), e.Message, "Operacion no completada", MessageBoxButtons.OK, MessageBoxIcon.Warning); } finally { if (!b.IsDisposed) b.Enabled = true; } }; return b;
    }
    public static FlowLayoutPanel Fila(params Control[] controles)
    { var p = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new(2), BackColor = Color.White }; p.Controls.AddRange(controles); return p; }
    public static DataGridView Tabla(string nombre) => new()
    {
        Name = nombre, AccessibleName = nombre, Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false, BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
        ColumnHeadersHeight = 38, EnableHeadersVisualStyles = false,
        ColumnHeadersDefaultCellStyle = new() { BackColor = Color.FromArgb(230, 237, 247), ForeColor = Color.FromArgb(31, 55, 89), Font = new("Segoe UI", 10, FontStyle.Bold), Padding = new(4) },
        DefaultCellStyle = new() { Font = new("Segoe UI", 10), SelectionBackColor = Color.FromArgb(215, 230, 250), SelectionForeColor = Color.Black, Padding = new(4) },
        AlternatingRowsDefaultCellStyle = new() { BackColor = Color.FromArgb(248, 250, 253) }, RowTemplate = { Height = 34 }
    };
    public static void Cargar<T>(DataGridView grid, IEnumerable<T> filas)
    {
        var anterior = Clave(grid.CurrentRow?.DataBoundItem); grid.DataSource = new ListaOrdenable<T>(filas.ToList());
        foreach (DataGridViewColumn columna in grid.Columns)
        {
            if (columna.Name is "Id" or "PedidoId" or "EventoId") columna.Visible = false;
            if (columna.ValueType == typeof(decimal)) { columna.DefaultCellStyle.Format = "N2"; columna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; }
            if (columna.ValueType == typeof(DateTimeOffset)) columna.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            columna.MinimumWidth = 65;
        }
        if (anterior is null) return;
        foreach (DataGridViewRow row in grid.Rows) if (Clave(row.DataBoundItem) == anterior) { row.Selected = true; grid.CurrentCell = row.Cells.Cast<DataGridViewCell>().First(c => c.Visible); break; }
    }
    private static string? Clave(object? fila)
    {
        if (fila is null) return null;
        foreach (var nombre in new[] { "Id", "Sku", "Codigo", "PedidoId", "EventoId" }) if (fila.GetType().GetProperty(nombre)?.GetValue(fila) is { } valor) return valor.ToString();
        return null;
    }
    public static Guid IdSeleccionado(DataGridView grid) => grid.CurrentRow?.DataBoundItem?.GetType().GetProperty("Id")?.GetValue(grid.CurrentRow.DataBoundItem) is Guid id ? id : throw new InvalidOperationException("Seleccione un registro.");
    public static bool Confirmar(Control control, string texto) => MessageBox.Show(control.FindForm(), texto, "Confirmar operacion", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    public sealed record Opcion(object Clave, string Texto);
    public static void Seleccionar<T>(ComboBox combo, IReadOnlyList<T> filas, Func<T, string> etiqueta, Func<T, object> clave)
    {
        var anterior = (combo.SelectedItem as Opcion)?.Clave; combo.DataSource = filas.Select(x => new Opcion(clave(x), etiqueta(x))).ToList(); combo.DisplayMember = nameof(Opcion.Texto);
        if (anterior is not null) for (int i = 0; i < combo.Items.Count; i++) if (combo.Items[i] is Opcion opcion && opcion.Clave.Equals(anterior)) { combo.SelectedIndex = i; break; }
    }
    public static object Elegido(ComboBox combo) => (combo.SelectedItem as Opcion)?.Clave ?? throw new InvalidOperationException("Seleccione " + combo.AccessibleName + ".");
}
internal sealed class ListaOrdenable<T>(IList<T> lista) : BindingList<T>(lista)
{
    private bool _ordenada; private PropertyDescriptor? _propiedad; private ListSortDirection _direccion;
    protected override bool SupportsSortingCore => true; protected override bool IsSortedCore => _ordenada;
    protected override PropertyDescriptor? SortPropertyCore => _propiedad; protected override ListSortDirection SortDirectionCore => _direccion;
    protected override void ApplySortCore(PropertyDescriptor propiedad, ListSortDirection direccion)
    {
        var lista = (List<T>)Items; lista.Sort((a, b) => { var va = propiedad.GetValue(a); var vb = propiedad.GetValue(b); var c = va is IComparable comparable ? comparable.CompareTo(vb) : string.Compare(va?.ToString(), vb?.ToString(), StringComparison.CurrentCulture); return direccion == ListSortDirection.Ascending ? c : -c; });
        _ordenada = true; _propiedad = propiedad; _direccion = direccion; OnListChanged(new(ListChangedType.Reset, -1));
    }
}
public abstract class VistaBase : UserControl, IVistaOperativa
{
    private readonly FormularioPrincipalMediator _mediador;
    protected readonly Panel Contenido = new() { Dock = DockStyle.Fill, Padding = new(2, 10, 2, 2) };
    protected readonly Label Estado = new() { Dock = DockStyle.Bottom, Height = 55, Padding = new(8), ForeColor = Color.FromArgb(30, 90, 65), BackColor = Color.FromArgb(230, 242, 235), AutoEllipsis = true };
    protected EstadoConsultas? Datos { get; private set; }
    public abstract string Nombre { get; }
    protected VistaBase(FormularioPrincipalMediator mediador, string titulo, string descripcion)
    {
        _mediador = mediador; Dock = DockStyle.Fill; BackColor = Ui.Fondo; Padding = new(20, 12, 20, 12);
        var cabecera = new Panel { Dock = DockStyle.Top, Height = 86 }; var t = Ui.Texto(titulo, 21, true); t.Location = new(0, 0);
        var d = Ui.Texto(descripcion); d.AutoSize = false; d.MaximumSize = Size.Empty; d.Location = new(2, 45); d.Size = new(cabecera.Width - 4, 40); d.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; cabecera.Controls.AddRange([t, d]);
        Controls.Add(Contenido); Controls.Add(Estado); Controls.Add(cabecera); Estado.Text = "Seleccione una operacion. Los datos se conservan en SQLite local.";
    }
    public void Actualizar(EstadoConsultas estado) { Datos = estado; Mostrar(estado); }
    protected abstract void Mostrar(EstadoConsultas datos);
    protected async Task Operacion(string mensaje, Func<Task> accion, bool refrescar = true)
    {
        try { await accion(); if (refrescar) await _mediador.RefrescarAsync(); Estado.ForeColor = Color.FromArgb(30, 90, 65); Estado.Text = mensaje; }
        catch (Exception e) { if (refrescar) { try { await _mediador.RefrescarAsync(); } catch { /* Conservar el error original. */ } } Estado.ForeColor = Color.Firebrick; Estado.Text = "No completado: " + e.GetBaseException().Message; }
    }
    protected Task RefrescarAsync() => _mediador.RefrescarAsync();
}
