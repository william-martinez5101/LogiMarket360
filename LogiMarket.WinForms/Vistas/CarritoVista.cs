using LogiMarket.Application.Consultas;
using LogiMarket.Application.Contratos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Presentacion;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.WinForms.Vistas;

public sealed class CarritoVista : VistaBase
{
    public override string Nombre => "Carrito / borrador";
    private readonly ComboBox _cliente = Ui.Combo("Cliente", 230), _entrega = Ui.Combo("Tipo entrega", 140), _elemento = Ui.Combo("Producto o kit", 285), _guardados = Ui.Combo("Borrador guardado", 245);
    private readonly NumericUpDown _cantidad = Ui.Numero("Cantidad", 0, 1, 1000000);
    private readonly DataGridView _lineas = Ui.Tabla("Lineas del borrador"); private readonly Label _totales = Ui.Texto("", 13, true);
    private readonly PedidosServicio _pedidos; private readonly ValoradorBorrador _valorador;
    private PedidoBorrador _borrador = new(); private HistorialBorrador _historial;
    private Guid? _editando; private long? _version; private bool _cargando, _bloqueado;
    public bool TieneCambios { get; private set; }
    public CarritoVista(FormularioPrincipalMediator mediador, PedidosServicio pedidos, ValoradorBorrador valorador, SesionUsuario sesion)
        : base(mediador, "Carrito y borrador", "Instantaneas de catalogo, ediciones recuperables y totales sin IVA.")
    {
        _pedidos = pedidos; _valorador = valorador; _historial = new(_borrador); _cantidad.Value = 1; _entrega.DataSource = Enum.GetValues<TipoEntrega>();
        var agregar = Ui.Boton("Agregar linea", () => Editar(b => { SincronizarCliente(b); var e = Datos!.Productos.Cast<IElementoCatalogo>().Concat(Datos.Kits).Single(x => x.Codigo == (string)Ui.Elegido(_elemento)); b.Agregar(new(NodoInstantanea.Capturar(e), (int)_cantidad.Value)); }), true);
        var cambiar = Ui.Boton("Cambiar cantidad", () => Editar(b => b.CambiarCantidad(Indice(), (int)_cantidad.Value)));
        var quitar = Ui.Boton("Quitar linea", () => Editar(b => b.Quitar(Indice())));
        var guardar = Ui.Boton("Guardar borrador", () => Guardar(false)); var validar = Ui.Boton("Guardar y validar", () => Guardar(true), true);
        var deshacer = Ui.Boton("Deshacer", () => MoverHistoria(false)); var rehacer = Ui.Boton("Rehacer", () => MoverHistoria(true));
        var nuevo = Ui.Boton("Nuevo borrador", Nuevo); var cargar = Ui.Boton("Cargar borrador", Cargar);
        var botones = new[] { agregar, cambiar, quitar, guardar, validar, deshacer, rehacer, nuevo, cargar };
        foreach (var b in botones) b.Enabled = sesion.Exigir().Rol is Rol.ADMIN or Rol.VENTAS;
        var resumen = Ui.Fila(_totales); resumen.Dock = DockStyle.Bottom;
        Contenido.Controls.Add(_lineas); Contenido.Controls.Add(resumen);
        Contenido.Controls.Add(Ui.Fila(deshacer, rehacer, cambiar, quitar, guardar, validar));
        Contenido.Controls.Add(Ui.Fila(Ui.Texto("Articulo"), _elemento, Ui.Texto("Cantidad"), _cantidad, agregar));
        Contenido.Controls.Add(Ui.Fila(Ui.Texto("Cliente"), _cliente, Ui.Texto("Entrega"), _entrega, nuevo));
        Contenido.Controls.Add(Ui.Fila(_guardados, cargar));
        _cliente.SelectedIndexChanged += async (_, _) => { if (!_cargando && _cliente.SelectedItem is Ui.Opcion) await Editar(SincronizarCliente); };
        _entrega.SelectedIndexChanged += async (_, _) => { if (!_cargando) await Editar(b => b.SeleccionarEntrega((TipoEntrega)_entrega.SelectedItem!)); };
    }
    private int Indice() => _lineas.CurrentRow?.Index ?? throw new InvalidOperationException("Seleccione una linea.");
    private void SincronizarCliente(PedidoBorrador b) { b.SeleccionarCliente((Guid)Ui.Elegido(_cliente)); b.SeleccionarEntrega((TipoEntrega)_entrega.SelectedItem!); }
    private Task Editar(Action<PedidoBorrador> accion) => Operacion("Borrador actualizado. Puede deshacer o rehacer.", () => {
        _historial.Editar(accion); TieneCambios = true; Renderizar(); return Task.CompletedTask;
    }, false);
    private Task MoverHistoria(bool adelante) => Operacion("Estado completo del borrador restaurado.", () => {
        if (adelante) _historial.Rehacer(); else _historial.Deshacer(); TieneCambios = true; Renderizar(); return Task.CompletedTask;
    }, false);
    protected override void Mostrar(EstadoConsultas d)
    {
        _cargando = true;
        Ui.Seleccionar(_cliente, d.Clientes.Where(x => x.Activo).ToList(), c => $"{c.Nombre} - {c.Categoria}", c => c.Id);
        Ui.Seleccionar(_elemento, d.Productos.Cast<IElementoCatalogo>().Concat(d.Kits).ToList(), e => $"{e.Codigo} - {e.Nombre}", e => e.Codigo);
        Ui.Seleccionar(_guardados, d.Pedidos.Where(p => p.Estado == EstadoPedido.BORRADOR).ToList(), p => $"{p.Id:N}"[..8] + " - " + p.ClienteNombre, p => p.Id);
        if (_borrador.ClienteId is null && !_bloqueado && _cliente.SelectedItem is Ui.Opcion o) { _borrador.SeleccionarCliente((Guid)o.Clave); _historial = new(_borrador); }
        if (_editando is Guid id && d.Pedidos.SingleOrDefault(p => p.Id == id) is { } actual)
        {
            if (!TieneCambios) _version = actual.Version;
            if (actual.Estado != EstadoPedido.BORRADOR && !_bloqueado) { _historial.Confirmado(); _bloqueado = true; TieneCambios = false; }
        }
        _cargando = false; Renderizar();
    }
    private void Renderizar()
    {
        _cargando = true;
        for (int i = 0; i < _cliente.Items.Count; i++) if (_cliente.Items[i] is Ui.Opcion opcion && opcion.Clave.Equals(_borrador.ClienteId)) _cliente.SelectedIndex = i;
        _entrega.SelectedItem = _borrador.Entrega; _cargando = false;
        Ui.Cargar(_lineas, _borrador.Lineas.Select((l, i) => new { Linea = i + 1, Articulo = l.Codigo, l.Cantidad, Precio = l.PrecioUnitario, l.Importe, UnidadesFisicas = l.Unidades }));
        var v = _valorador.Valorar(_borrador, Datos?.Clientes.SingleOrDefault(c => c.Id == _borrador.ClienteId));
        _totales.Text = v is null ? "Agregue lineas para calcular el pedido." : $"Subtotal USD {v.Subtotal:N2}   |   {v.Promocion}: -{v.Descuento:N2}   |   Entrega USD {v.Tarifa:N2}   |   TOTAL USD {v.Total:N2}\nUnidades fisicas: {v.Unidades}  |  {(_bloqueado ? "Registrado y bloqueado" : "Edicion de borrador")}  |  {(_editando?.ToString() ?? "Sin registrar")}";
    }
    private async Task Guardar(bool validar)
    {
        if (validar && !Ui.Confirmar(this, "¿Guardar y validar este pedido con sus precios y composicion actuales?")) return;
        await Operacion(validar ? "Pedido VALIDADO. Puede continuar en Cobro." : "Borrador guardado en SQLite.", async () => {
            if (_bloqueado) throw new InvalidOperationException("Cree un nuevo borrador para otra venta.");
            SincronizarCliente(_borrador); var id = await _pedidos.RegistrarAsync(_borrador, _editando, _version); _editando = id; TieneCambios = false;
            if (validar) { await _pedidos.ValidarAsync(id); _historial.Confirmado(); _bloqueado = true; }
        });
    }
    private Task Nuevo()
    {
        if (TieneCambios && !Ui.Confirmar(this, "¿Descartar los cambios del borrador actual y crear uno nuevo?")) return Task.CompletedTask;
        _borrador = new(); _historial = new(_borrador); _editando = null; _version = null; _bloqueado = false; TieneCambios = false;
        if (_cliente.SelectedItem is Ui.Opcion) SincronizarCliente(_borrador); _historial = new(_borrador); Renderizar(); return Task.CompletedTask;
    }
    private Task Cargar() => Operacion("Borrador recuperado desde SQLite.", () => {
        if (TieneCambios && !Ui.Confirmar(this, "¿Reemplazar los cambios actuales por el borrador guardado?")) return Task.CompletedTask;
        var p = Datos!.Pedidos.Single(x => x.Id == (Guid)Ui.Elegido(_guardados)); _borrador = new(); _borrador.SeleccionarCliente(p.ClienteId); _borrador.SeleccionarEntrega(p.Entrega.Tipo);
        foreach (var l in p.Lineas) _borrador.Agregar(l); _historial = new(_borrador); _editando = p.Id; _version = p.Version; _bloqueado = false; TieneCambios = false; Renderizar(); return Task.CompletedTask;
    }, false);
}
