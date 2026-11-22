using LogiMarket.Application.Consultas;
using LogiMarket.Application.Contratos;
using LogiMarket.Application.Gestion;
using LogiMarket.Application.Presentacion;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Catalogo;

namespace LogiMarket.WinForms.Vistas;

public sealed class CatalogoVista : VistaBase
{
    public override string Nombre => "Catalogo y kits";
    private readonly DataGridView _tabla = Ui.Tabla("Catalogo"); private readonly TreeView _arbol = new() { Dock = DockStyle.Fill, Font = new("Segoe UI", 11), AccessibleName = "Composicion del kit" };
    private readonly TextBox _buscar = Ui.Campo("Buscar producto o kit"); private readonly Label _detalle = Ui.Texto(""); private readonly CatalogoServicio _servicio;
    private sealed record Fila(string Codigo, string Nombre, string Tipo, decimal Precio, int? Stock);
    public CatalogoVista(FormularioPrincipalMediator mediador, CatalogoServicio servicio, SesionUsuario sesion, CatalogoVisualFlyweightFactory visuales)
        : base(mediador, "Catalogo y kits", "Productos individuales, estructura anidada, precios y existencias.")
    {
        _servicio = servicio; var dividir = new SplitContainer { Size = new(1100, 600), Dock = DockStyle.Fill, SplitterDistance = 720, Panel2MinSize = 240 };
        dividir.Panel1.Controls.Add(_tabla); dividir.Panel2.Controls.Add(_arbol); var detalle = Ui.Fila(_detalle); detalle.Dock = DockStyle.Bottom; dividir.Panel2.Controls.Add(detalle);
        var nuevoP = Ui.Boton("Nuevo producto", () => EditarProducto(null)); var editar = Ui.Boton("Editar seleccionado", EditarSeleccion);
        var nuevoK = Ui.Boton("Nuevo kit", () => EditarKit(null)); var eliminar = Ui.Boton("Eliminar", Eliminar);
        foreach (var boton in new[] { nuevoP, editar, nuevoK, eliminar }) boton.Enabled = sesion.Exigir().Rol == Rol.ADMIN;
        Contenido.Controls.Add(dividir); Contenido.Controls.Add(Ui.Fila(Ui.Texto("Buscar"), _buscar, nuevoP, nuevoK, editar, eliminar));
        _buscar.TextChanged += (_, _) => Filtrar(); _tabla.SelectionChanged += (_, _) => MostrarArbol();
    }
    protected override void Mostrar(EstadoConsultas d) => Filtrar();
    private void Filtrar()
    {
        if (Datos is null) return; var texto = _buscar.Text.Trim();
        Ui.Cargar(_tabla, Datos.Productos.Select(p => new Fila(p.Sku, p.Nombre, "PRODUCTO", p.Precio, p.Stock)).Concat(Datos.Kits.Select(k => new Fila(k.Codigo, k.Nombre, "KIT", k.CalcularPrecio(), null)))
            .Where(x => x.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase) || x.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)));
        MostrarArbol();
    }
    private void MostrarArbol()
    {
        _arbol.Nodes.Clear(); if (Datos is null || _tabla.CurrentRow?.DataBoundItem is not Fila fila) return;
        var elemento = Datos.Productos.Cast<IElementoCatalogo>().Concat(Datos.Kits).Single(x => x.Codigo == fila.Codigo);
        TreeNode Nodo(IElementoCatalogo e, int cantidad) { var nodo = new TreeNode($"{e.Codigo} - {e.Nombre} x{cantidad}"); if (e is KitCompuesto kit) foreach (var c in kit.Componentes) nodo.Nodes.Add(Nodo(c.Elemento, c.Cantidad)); return nodo; }
        _arbol.Nodes.Add(Nodo(elemento, 1)); _arbol.ExpandAll(); var peso = new PesoLogisticoVisitor(); var desglose = new DesgloseFisicoVisitor(); elemento.Aceptar(peso); elemento.Aceptar(desglose);
        _detalle.Text = $"Peso: {peso.PesoKg:N2} kg\nFisicos: {string.Join(", ", desglose.Unidades.Select(x => x.Key + " x" + x.Value))}";
    }
    private Task EditarSeleccion() => _tabla.CurrentRow?.DataBoundItem is Fila f ? (f.Tipo == "KIT" ? EditarKit(f.Codigo) : EditarProducto(f.Codigo)) : Task.CompletedTask;
    private async Task EditarProducto(string? sku)
    {
        var p = Datos?.Productos.SingleOrDefault(x => x.Sku == sku); var codigo = Ui.Campo("SKU"); codigo.Text = p?.Sku ?? ""; codigo.ReadOnly = p is not null;
        var nombre = Ui.Campo("Nombre"); nombre.Text = p?.Nombre ?? ""; var precio = Ui.Numero("Precio USD", 2, .01m); precio.Value = p?.Precio ?? .01m;
        var peso = Ui.Numero("Peso kg", 3); peso.Value = p?.PesoKg ?? 0; var stock = Ui.Numero("Stock inicial"); stock.Value = p?.Stock ?? 0; stock.Enabled = p is null;
        using var dialogo = new DialogoEdicion(p is null ? "Nuevo producto" : "Editar producto", () => _servicio.GuardarProductoAsync(codigo.Text, nombre.Text, precio.Value, peso.Value, (int)stock.Value));
        dialogo.Agregar("SKU unico", codigo); dialogo.Agregar("Nombre", nombre); dialogo.Agregar("Precio USD", precio); dialogo.Agregar("Peso kg", peso); dialogo.Agregar("Stock inicial", stock);
        if (dialogo.ShowDialog(FindForm()) == DialogResult.OK) await RefrescarAsync();
    }
    private async Task EditarKit(string? codigoKit)
    {
        var kit = Datos?.Kits.SingleOrDefault(x => x.Codigo == codigoKit); var codigo = Ui.Campo("Codigo kit"); codigo.Text = kit?.Codigo ?? ""; codigo.ReadOnly = kit is not null;
        var nombre = Ui.Campo("Nombre kit"); nombre.Text = kit?.Nombre ?? ""; var componentes = Ui.Campo("Componentes SKU:cantidad"); componentes.Multiline = true; componentes.Height = 135;
        componentes.Text = kit is null ? "" : string.Join(Environment.NewLine, kit.Componentes.Select(c => c.Elemento.Codigo + ":" + c.Cantidad));
        using var dialogo = new DialogoEdicion("Composicion de kit", async () => {
            var partes = componentes.Text.Split([';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var lista = partes.Select(x => { var p = x.Split(':'); if (p.Length != 2 || !int.TryParse(p[1], out var n)) throw new FormatException("Use CODIGO:cantidad, una linea por componente."); return new ComposicionKit(p[0].Trim().ToUpperInvariant(), n); }).ToList();
            await _servicio.GuardarKitAsync(codigo.Text, nombre.Text, lista);
        });
        dialogo.Agregar("Codigo unico", codigo); dialogo.Agregar("Nombre", nombre); dialogo.Agregar("Componentes\nP01:1\nKIT_OFICINA:2", componentes);
        if (dialogo.ShowDialog(FindForm()) == DialogResult.OK) await RefrescarAsync();
    }
    private async Task Eliminar()
    {
        if (_tabla.CurrentRow?.DataBoundItem is not Fila f || !Ui.Confirmar(this, $"¿Eliminar {f.Codigo}? Se rechazara si tiene referencias.")) return;
        await Operacion("Elemento eliminado.", () => f.Tipo == "KIT" ? _servicio.EliminarKitAsync(f.Codigo) : _servicio.EliminarProductoAsync(f.Codigo));
    }
}
