using LogiMarket.Application.Consultas;
using LogiMarket.Application.Contratos;
using LogiMarket.Application.Gestion;
using LogiMarket.Application.Presentacion;
using LogiMarket.Application.Seguridad;

namespace LogiMarket.WinForms.Vistas;

public sealed class InventarioVista : VistaBase
{
    public override string Nombre => "Inventario";
    private readonly DataGridView _stock = Ui.Tabla("Existencias"), _movimientos = Ui.Tabla("Movimientos de inventario");
    private readonly ComboBox _producto = Ui.Combo("SKU para ajuste", 250); private readonly TextBox _motivo = Ui.Campo("Motivo del ajuste", 270), _buscar = Ui.Campo("Filtrar inventario", 180);
    private readonly NumericUpDown _cantidad = Ui.Numero("Variacion de stock", 0, -1000000, 1000000);
    public InventarioVista(FormularioPrincipalMediator mediador, CatalogoServicio catalogo, SesionUsuario sesion)
        : base(mediador, "Inventario y movimientos", "Stock disponible y trazabilidad por SKU. Los ajustes requieren motivo y permiso de BODEGA / ADMIN.")
    {
        var dividir = new SplitContainer { Size = new(1100, 600), Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 260 }; dividir.Panel1.Controls.Add(_stock); dividir.Panel2.Controls.Add(_movimientos);
        var ajustar = Ui.Boton("Registrar ajuste", async () => {
            if (Ui.Confirmar(this, "¿Registrar el ajuste de inventario con el motivo indicado?")) await Operacion("Ajuste y movimiento registrados.", () => catalogo.AjustarStockAsync((string)Ui.Elegido(_producto), (int)_cantidad.Value, _motivo.Text));
        }, true); ajustar.Enabled = sesion.Exigir().Rol is Rol.ADMIN or Rol.BODEGA;
        Contenido.Controls.Add(dividir); Contenido.Controls.Add(Ui.Fila(_producto, Ui.Texto("Variacion +/-"), _cantidad, _motivo, ajustar));
        Contenido.Controls.Add(Ui.Fila(Ui.Texto("Buscar SKU o nombre"), _buscar)); _buscar.TextChanged += (_, _) => Filtrar();
    }
    protected override void Mostrar(EstadoConsultas d)
    { Ui.Seleccionar(_producto, d.Productos, p => p.Sku + " - " + p.Nombre, p => p.Sku); Filtrar(); }
    private void Filtrar()
    {
        if (Datos is null) return; var filtro = _buscar.Text.Trim();
        Ui.Cargar(_stock, Datos.Productos.Where(p => p.Sku.Contains(filtro, StringComparison.OrdinalIgnoreCase) || p.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase)).Select(p => new { p.Sku, p.Nombre, Disponible = p.Stock, Alerta = p.Stock <= 3 ? "BAJO" : "OK" }));
        Ui.Cargar(_movimientos, Datos.Movimientos.Where(m => m.Sku.Contains(filtro, StringComparison.OrdinalIgnoreCase)).Select(m => new { m.Id, m.Sku, m.Cantidad, m.Motivo, m.Referencia, Fecha = m.Fecha.ToLocalTime() }));
    }
}
