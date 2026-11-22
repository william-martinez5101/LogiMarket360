using LogiMarket.Application.Consultas;
using LogiMarket.Application.Gestion;
using LogiMarket.Application.Presentacion;
using LogiMarket.Application.Seguridad;
using LogiMarket.Application.Contratos;
using LogiMarket.Domain.Clientes;

namespace LogiMarket.WinForms.Vistas;

public sealed class ClientesVista : VistaBase
{
    public override string Nombre => "Clientes";
    private readonly DataGridView _tabla = Ui.Tabla("Clientes"); private readonly TextBox _buscar = Ui.Campo("Buscar cliente"); private readonly ClientesServicio _servicio;
    public ClientesVista(FormularioPrincipalMediator mediador, ClientesServicio servicio, SesionUsuario sesion) : base(mediador, "Gestion de clientes", "Registro, categoria comercial y activacion del cliente.")
    {
        _servicio = servicio; var nuevo = Ui.Boton("Nuevo cliente", () => Editar(null), true); var editar = Ui.Boton("Editar cliente", () => Editar(Ui.IdSeleccionado(_tabla)));
        nuevo.Enabled = editar.Enabled = sesion.Exigir().Rol is Rol.ADMIN or Rol.VENTAS;
        Contenido.Controls.Add(_tabla); Contenido.Controls.Add(Ui.Fila(Ui.Texto("Buscar"), _buscar, nuevo, editar)); _buscar.TextChanged += (_, _) => Filtrar();
    }
    protected override void Mostrar(EstadoConsultas datos) => Filtrar();
    private void Filtrar() { if (Datos is not null) Ui.Cargar(_tabla, Datos.Clientes.Where(c => c.Nombre.Contains(_buscar.Text.Trim(), StringComparison.OrdinalIgnoreCase)).Select(c => new { c.Id, c.Nombre, c.Categoria, c.Activo })); }
    private async Task Editar(Guid? id)
    {
        var cliente = Datos?.Clientes.SingleOrDefault(x => x.Id == id); var nombre = Ui.Campo("Nombre cliente"); nombre.Text = cliente?.Nombre ?? "";
        var categoria = Ui.Combo("Categoria cliente"); categoria.DataSource = Enum.GetValues<CategoriaCliente>(); categoria.SelectedItem = cliente?.Categoria ?? CategoriaCliente.Normal;
        var activo = new CheckBox { Text = "Cliente activo", Checked = cliente?.Activo ?? true, AutoSize = true };
        using var dialogo = new DialogoEdicion(cliente is null ? "Nuevo cliente" : "Editar cliente", async () => { await _servicio.GuardarAsync(id, nombre.Text, (CategoriaCliente)categoria.SelectedItem!, activo.Checked); });
        dialogo.Agregar("Nombre", nombre); dialogo.Agregar("Categoria", categoria); dialogo.Agregar("Estado", activo);
        if (dialogo.ShowDialog(FindForm()) == DialogResult.OK) await RefrescarAsync();
    }
}
