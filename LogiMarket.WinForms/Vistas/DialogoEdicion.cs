namespace LogiMarket.WinForms.Vistas;

internal sealed class DialogoEdicion : Form
{
    private readonly TableLayoutPanel _campos = new() { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true, Padding = new(18) };
    private readonly Label _error = new() { Dock = DockStyle.Bottom, Height = 65, ForeColor = Color.Firebrick, Padding = new(15) };
    private readonly ErrorProvider _validacion;
    public DialogoEdicion(string titulo, Func<Task> guardar)
    {
        Text = titulo; ClientSize = new(600, 450); MinimumSize = new(560, 420); StartPosition = FormStartPosition.CenterParent;
        Font = new("Segoe UI", 10); BackColor = Ui.Fondo; AutoScaleMode = AutoScaleMode.Dpi; _validacion = new() { ContainerControl = this };
        _campos.ColumnStyles.Add(new(SizeType.Percent, 30)); _campos.ColumnStyles.Add(new(SizeType.Percent, 70));
        var botones = Ui.Fila(Ui.Boton("Guardar", async () => {
            _validacion.Clear(); foreach (Control control in _campos.Controls)
                if (control is TextBox campo && !campo.Multiline && string.IsNullOrWhiteSpace(campo.Text)) { _validacion.SetError(campo, "Campo obligatorio."); campo.Focus(); return; }
            try { await guardar(); DialogResult = DialogResult.OK; Close(); } catch (Exception e) { _error.Text = e.GetBaseException().Message; }
        }, true), Ui.Boton("Cancelar", () => { DialogResult = DialogResult.Cancel; Close(); return Task.CompletedTask; })); botones.Dock = DockStyle.Bottom;
        Controls.Add(_campos); Controls.Add(_error); Controls.Add(botones);
        FormClosed += (_, _) => _validacion.Dispose();
    }
    public void Agregar(string etiqueta, Control campo)
    {
        var fila = _campos.RowCount++; _campos.RowStyles.Add(new(SizeType.AutoSize));
        var label = Ui.Texto(etiqueta); label.MaximumSize = new(150, 0);
        campo.Anchor = AnchorStyles.Left | AnchorStyles.Right; campo.Width = 340; campo.TabIndex = fila;
        _campos.Controls.Add(label, 0, fila); _campos.Controls.Add(campo, 1, fila);
    }
}
