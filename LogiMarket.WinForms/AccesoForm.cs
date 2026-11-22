using LogiMarket.Application.Seguridad;
using LogiMarket.WinForms.Vistas;

namespace LogiMarket.WinForms;

public sealed class AccesoForm : Form
{
    public AccesoForm(Autenticacion acceso)
    {
        Text = "LogiMarket 360 - Acceso"; ClientSize = new(540, 470); StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; Font = new("Segoe UI", 11); BackColor = Ui.Fondo; AutoScaleMode = AutoScaleMode.Dpi;
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new(50, 30, 50, 20), RowCount = 10, ColumnCount = 1 };
        var usuario = Ui.Campo("Usuario", 390); usuario.Text = "admin.demo";
        var clave = Ui.Campo("Contrasena", 390); clave.UseSystemPasswordChar = true;
        var error = Ui.Texto(""); error.ForeColor = Color.Firebrick; error.MaximumSize = new(390, 65);
        var ingresar = Ui.Boton("Ingresar", async () => { try { await acceso.IngresarAsync(usuario.Text, clave.Text); DialogResult = DialogResult.OK; Close(); } catch (Exception e) { error.Text = e.Message; clave.SelectAll(); clave.Focus(); } }, true);
        ingresar.Width = 390; AcceptButton = ingresar;
        panel.Controls.Add(Ui.Texto("LOGIMARKET 360", 24, true)); panel.Controls.Add(Ui.Texto("Ventas y logistica | Escritorio local"));
        panel.Controls.Add(Ui.Texto("Usuario")); panel.Controls.Add(usuario); panel.Controls.Add(Ui.Texto("Contrasena")); panel.Controls.Add(clave);
        panel.Controls.Add(ingresar); panel.Controls.Add(error); panel.Controls.Add(Ui.Texto("Cuentas ficticias: admin.demo, ventas.demo,\nbodega.demo, consulta.demo | Clave: Demo360!", 9)); Controls.Add(panel);
        Shown += (_, _) => clave.Focus();
    }
}
