using LogiMarket.Application.Presentacion;
using LogiMarket.Application.Seguridad;
using LogiMarket.WinForms.Vistas;

namespace LogiMarket.WinForms;

public sealed class VentanaPrincipal : Form
{
    public bool VolverAIngresar { get; private set; }
    public VentanaPrincipal(IEnumerable<VistaBase> vistas, FormularioPrincipalMediator mediador, SesionUsuario sesion, RutasAplicacion rutas)
    {
        var paginas = vistas.ToArray();
        Text = "LogiMarket 360 - Ventas y logistica"; Size = new(1450, 900); MinimumSize = new(1120, 720);
        StartPosition = FormStartPosition.CenterScreen; Font = new("Segoe UI", 10); AutoScaleMode = AutoScaleMode.Dpi;
        var cabecera = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Ui.Azul, Padding = new(20, 15, 20, 10) };
        var titulo = Ui.Texto("LOGIMARKET 360", 20, true); titulo.ForeColor = Color.White; titulo.Dock = DockStyle.Left;
        var perfil = Ui.Texto($"{sesion.Exigir().Usuario}  |  {sesion.Exigir().Rol}", 11); perfil.ForeColor = Color.White; perfil.Dock = DockStyle.Right;
        cabecera.Controls.AddRange([titulo, perfil]);
        var navegacion = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 215, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.FromArgb(231, 237, 247), Padding = new(7, 15, 7, 8) };
        var contenido = new Panel { Dock = DockStyle.Fill };
        foreach (var vista in paginas)
        {
            mediador.Registrar(vista); contenido.Controls.Add(vista); vista.Visible = false;
            var boton = Ui.Boton(vista.Nombre, () => { foreach (var p in paginas) p.Visible = p == vista; vista.BringToFront(); return Task.CompletedTask; });
            boton.Width = 190; boton.TextAlign = ContentAlignment.MiddleLeft; navegacion.Controls.Add(boton);
        }
        navegacion.Controls.Add(Ui.Boton("Actualizar datos", () => mediador.RefrescarAsync()));
        navegacion.Controls.Add(Ui.Boton("Cerrar sesion", () => { VolverAIngresar = true; Close(); return Task.CompletedTask; }));
        var pie = new Label { Dock = DockStyle.Bottom, Height = 34, Padding = new(12, 7, 8, 4), Text = "SQLite local: " + rutas.BaseDatos, AutoEllipsis = true, BackColor = Color.White, AccessibleName = "Ubicacion de la base" };
        Controls.Add(contenido); Controls.Add(navegacion); Controls.Add(pie); Controls.Add(cabecera);
        Shown += async (_, _) => { try { await mediador.RefrescarAsync(); paginas[0].Visible = true; paginas[0].BringToFront(); } catch (Exception e) { MessageBox.Show(this, e.Message, "Error de consulta"); } };
        FormClosing += (_, evento) => {
            var carrito = paginas.OfType<CarritoVista>().Single();
            if (carrito.TieneCambios && !Ui.Confirmar(this, "Hay cambios en el borrador. Guarde el borrador antes de salir para conservarlos. ¿Salir de todos modos?")) { evento.Cancel = true; VolverAIngresar = false; }
        };
        FormClosed += (_, _) => { foreach (var vista in paginas) mediador.Retirar(vista); };
    }
}
