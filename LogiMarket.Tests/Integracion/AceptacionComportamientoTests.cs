using LogiMarket.Application.Contratos;
using LogiMarket.Application.Consultas;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Presentacion;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.Tests.Integracion;

public sealed partial class AceptacionTests
{
    [Fact] public async Task P23_ChainPrimerError()
    {
        await using var e = await Escenario.CrearAsync(); var d = await e.Consultas.CargarAsync(); var cadena = new CadenaConfirmacion();
        var contexto = new ContextoValidacion(null, [], d.Productos, e.Sesion, EstadoPedido.VALIDADO);
        var primero = cadena.Evaluar(contexto); Assert.Equal("CLIENTE", primero!.Codigo);
        contexto = contexto with { Cliente = d.Clientes.First() }; var segundo = cadena.Evaluar(contexto); Assert.Equal("LINEAS", segundo!.Codigo);
        contexto = contexto with { Lineas = [new(NodoInstantanea.Capturar(d.Kits.Single(x => x.Codigo == "KIT_TELETRABAJO")), 6)] };
        var tercero = cadena.Evaluar(contexto); Assert.Equal("STOCK", tercero!.Codigo); Assert.Contains("P02", tercero.Mensaje); Assert.Empty(e.Diario.Leer()); Assert.Empty(e.Log.Leer());
        await e.EvidenciaAsync("P23", "CLIENTE, luego LINEAS, luego STOCK/P02; sin cobro", new { primero, segundo, tercero });
    }
    [Fact] public async Task P24_CommandInvocadorConRegistro()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.ValidadoAsync();
        var pago = await e.Invocador.EjecutarAsync(new ConfirmarPedidoCommand(e.Facade, id, "command"));
        var d = await e.Consultas.CargarAsync(); Assert.Equal(59, pago.Importe);
        Assert.Contains(d.Auditoria, a => a.Tipo == "COMMAND" && a.Mensaje.Contains("ConfirmarPedidoCommand") && a.Mensaje.Contains("INICIO"));
        Assert.Contains(d.Auditoria, a => a.Tipo == "COMMAND" && a.Mensaje.Contains("ConfirmarPedidoCommand") && a.Mensaje.Contains("FIN"));
        await e.EvidenciaAsync("P24", "Mismo ConfirmarPedidoCommand que usa UI; historial con entrada y resultado", d.Auditoria);
    }
    [Fact] public async Task P25_InterpreterFiltroOficial()
    {
        await using var e = await Escenario.CrearAsync(); var valido = await e.PagadoAsync(); await e.BorradorAsync("Luis", TipoEntrega.ESTANDAR, ("P05", 1));
        var r = await e.Consultas.FiltrarPedidosAsync("ESTADO = PAGADO AND TOTAL >= 50"); Assert.Single(r); Assert.Equal(valido, r[0].Id);
        await e.EvidenciaAsync("P25", "Solo pedidos PAGADO con total >=50", r.Select(x => new { x.Id, x.Estado, x.Valores.Total }));
    }
    [Fact] public async Task P26_InterpreterPrecedenciaYSintaxis()
    {
        await using var e = await Escenario.CrearAsync(); var ana = await e.PagadoAsync(); await e.BorradorAsync("Luis", TipoEntrega.ESTANDAR, ("P05", 1));
        var caro = await e.ValidadoAsync("Luis", TipoEntrega.EXPRES, ("P04", 2), ("P05", 2));
        var r1 = await e.Consultas.FiltrarPedidosAsync("CLIENTE = 'Ana' OR ESTADO = VALIDADO AND TOTAL >= 100");
        var r2 = await e.Consultas.FiltrarPedidosAsync("(CLIENTE = 'Ana' OR ESTADO = VALIDADO) AND TOTAL >= 100");
        Assert.Equal(2, r1.Count); Assert.Single(r2); Assert.Equal(caro, r2[0].Id);
        var error = await Assert.ThrowsAsync<FormatException>(() => e.Consultas.FiltrarPedidosAsync("ESTADO PAGADO AND")); Assert.Contains("posicion", error.Message);
        await Assert.ThrowsAsync<FormatException>(() => e.Consultas.FiltrarPedidosAsync("TOTAL >= 0; DROP TABLE Pedidos"));
        await e.EvidenciaAsync("P26", "AND precede OR; parentesis cambian resultado 2->1; error legible sin SQL", new { SinParentesis = r1.Select(x => x.Id), ConParentesis = r2.Select(x => x.Id), error.Message });
    }
    [Fact] public async Task P27_IteratorFisicoPropagaCantidad()
    {
        await using var e = await Escenario.CrearAsync(); var kit = (await e.Consultas.CargarAsync()).Kits.Single(x => x.Codigo == "KIT_TELETRABAJO");
        using var i = new IteradorFisico(kit); var codigos = new List<string>(); while (i.MoveNext()) codigos.Add(i.Current.Producto.Sku);
        Assert.Equal(new[] { "P01", "P02", "P03" }, codigos); Assert.False(i.MoveNext()); Assert.Throws<InvalidOperationException>(() => i.Current);
        using var doble = new IteradorFisico(kit, 2); while (doble.MoveNext()) Assert.Equal(2, doble.Current.Cantidad);
        await e.EvidenciaAsync("P27", "P01,P02,P03; fin estable; multiplicador 2 llega a todas las hojas", codigos);
    }
    private sealed class VistaPrueba(string nombre, Action<EstadoConsultas> aplicar) : IVistaOperativa
    { public string Nombre => nombre; public int Refrescos { get; private set; } public void Actualizar(EstadoConsultas estado) { Refrescos++; aplicar(estado); } }
    [Fact] public async Task P28_MediatorRefrescaTresVistas()
    {
        await using var e = await Escenario.CrearAsync(); var mediador = new FormularioPrincipalMediator(e.Consultas);
        int stock = 0, pedidos = 0, pagos = 0;
        var catalogo = new VistaPrueba("Catalogo", d => stock = d.Productos.Single(p => p.Sku == "P01").Stock);
        var lista = new VistaPrueba("Pedidos", d => pedidos = d.Pedidos.Count); var metricas = new VistaPrueba("Metricas", d => pagos = e.Metricas.Pagos);
        mediador.Registrar(catalogo); mediador.Registrar(lista); mediador.Registrar(metricas); await mediador.RefrescarAsync();
        await e.PagadoAsync(); await e.Outbox.ProcesarAsync(); await mediador.RefrescarAsync();
        Assert.Equal(7, stock); Assert.Equal(1, pedidos); Assert.Equal(1, pagos); Assert.Equal(2, catalogo.Refrescos); Assert.Equal(2, lista.Refrescos); Assert.Equal(2, metricas.Refrescos);
        await e.EvidenciaAsync("P28", "Tres vistas refrescadas por mediador despues de confirmar", new { stock, pedidos, pagos, catalogo.Refrescos });
    }
    [Fact] public async Task P29_MementoTresEdicionesDosUndoUnRedo()
    {
        await using var e = await Escenario.CrearAsync(); var d = await e.Consultas.CargarAsync(); var b = new PedidoBorrador(); var h = new HistorialBorrador(b);
        var cliente = d.Clientes.Single(x => x.Nombre == "Ana");
        h.Editar(x => x.SeleccionarCliente(cliente.Id)); h.Editar(x => x.Agregar(new(NodoInstantanea.Capturar(d.Kits.Single(k => k.Codigo == "KIT_TELETRABAJO")), 1)));
        h.Editar(x => { x.CambiarCantidad(0, 3); x.SeleccionarEntrega(TipoEntrega.EXPRES); });
        h.Deshacer(); h.Deshacer(); Assert.Empty(b.Lineas); Assert.Equal(cliente.Id, b.ClienteId); Assert.Equal(TipoEntrega.ESTANDAR, b.Entrega);
        h.Rehacer(); Assert.Single(b.Lineas); Assert.Equal(1, b.Lineas[0].Cantidad); Assert.Equal(2, b.Lineas[0].Elemento.Hijos.Count);
        var id = await e.Pedidos.RegistrarAsync(b); await e.Pedidos.ValidarAsync(id); await e.Facade.ConfirmarAsync(id, "memento"); h.Confirmado();
        Assert.False(h.PuedeDeshacer); Assert.Throws<InvalidOperationException>(() => h.Deshacer());
        await e.EvidenciaAsync("P29", "Restauracion profunda completa y bloqueo despues de pago", new { b.ClienteId, b.Entrega, Cantidad = b.Lineas[0].Cantidad, Estados = h.CantidadEstados });
    }
    [Fact] public async Task P30_ObserverTresSuscriptoresUnaVez()
    {
        await using var e = await Escenario.CrearAsync(); await e.PagadoAsync(); await e.Outbox.ProcesarAsync(); await e.Outbox.ProcesarAsync();
        var d = await e.Consultas.CargarAsync(); Assert.Single(d.Avisos); Assert.Single(d.Auditoria.Where(x => x.Tipo == "EVENTO")); Assert.Equal(1, e.Metricas.Pagos);
        Assert.NotNull(d.Outbox.Single().Procesado); await using var u = await e.Unidades.AbrirAsync(false);
        Assert.Single(await u.Eventos.MetricasAsync()); foreach (var nombre in new[] { "AUDITORIA", "METRICAS", "NOTIFICACION" }) Assert.True(await u.Eventos.EntregadoAsync(d.Outbox.Single().Evento.Id, nombre));
        await e.EvidenciaAsync("P30", "Notificacion, auditoria y metrica exactamente una vez con doble procesamiento", new { Avisos = d.Avisos.Count, Metricas = e.Metricas.Pagos, AuditoriasEvento = d.Auditoria.Count(x => x.Tipo == "EVENTO") });
    }
    [Fact] public async Task P31_StateSecuenciaYRechazos()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.PagadoAsync(); await Assert.ThrowsAsync<InvalidOperationException>(() => e.Pedidos.CancelarAsync(id));
        await e.Pedidos.DespacharAsync(id); await Assert.ThrowsAsync<InvalidOperationException>(() => e.Pedidos.DespacharAsync(id)); await e.Pedidos.EntregarAsync(id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Pedidos.EntregarAsync(id)); var p = (await e.Consultas.CargarAsync()).Pedidos.Single();
        Assert.Equal(new[] { EstadoPedido.BORRADOR, EstadoPedido.VALIDADO, EstadoPedido.PAGADO, EstadoPedido.DESPACHADO, EstadoPedido.ENTREGADO }, p.Historial.Select(x => x.Hasta));
        await e.EvidenciaAsync("P31", "Ciclo completo; cancelacion/repeticiones invalidas sin historial adicional", p.Historial);
    }
    private sealed class NuevaPromocion : IDescuento
    { public string Nombre => "PROMO20"; public int Prioridad => 200; public decimal Tasa => .20m; public bool Aplica(ContextoPromocion c) => c.Subtotal >= 50; }
    [Fact] public async Task P32_StrategyExtensibleYPrioridad()
    {
        await using var e = await Escenario.CrearAsync(); var d = await e.Consultas.CargarAsync(); var kit = d.Kits.Single(x => x.Codigo == "KIT_TELETRABAJO"); var linea = new LineaPedido(NodoInstantanea.Capturar(kit), 1);
        Assert.Equal(6, e.Calculadora.Calcular(CategoriaCliente.Vip, [linea], 5).Descuento);
        Assert.Equal(15, e.Calculadora.Calcular(CategoriaCliente.Mayorista, [new(NodoInstantanea.Capturar(d.Productos.Single(x => x.Sku == "P03")), 10)], 5).Descuento);
        Assert.Equal(0, e.Calculadora.Calcular(CategoriaCliente.Normal, [linea], 5).Descuento);
        var extendida = new CalculadoraPedido([new Mayorista15(), new Vip10(), new SinDescuento(), new NuevaPromocion()]); var r = extendida.Calcular(CategoriaCliente.Vip, [linea], 5); Assert.Equal(12, r.Descuento);
        Assert.Equal(36, e.Calculadora.Calcular(CategoriaCliente.Vip, [linea.Copiar(4)], 5).Descuento);
        var redondeo = e.Calculadora.Calcular(CategoriaCliente.Vip, [new(new("ROUND", "Centavos", 1, .05m, 0, []), 1)], 5); Assert.Equal(.01m, redondeo.Descuento);
        await e.EvidenciaAsync("P32", "VIP6/MAYORISTA15/NORMAL0; nueva politica12; prioridad y AwayFromZero", new { Extension = r, redondeo });
    }
    [Fact] public async Task P33_TemplatePlazosYCondicion()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.EntregadoAsync(); e.Reloj.Ahora = e.Reloj.Ahora.AddDays(20);
        var aceptada = await e.Devoluciones.ProcesarAsync(new(id, "DEFECTO", false)); Assert.Equal(54, aceptada.Reembolso);
        await using var r = await Escenario.CrearAsync(); var otro = await r.EntregadoAsync(); r.Reloj.Ahora = r.Reloj.Ahora.AddDays(10);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => r.Devoluciones.ProcesarAsync(new(otro, "ARREPENTIMIENTO", true))); var d = await r.Consultas.CargarAsync();
        Assert.Empty(d.Devoluciones); Assert.Equal(3, d.Movimientos.Count); Assert.Equal(7, d.Productos.Single(x => x.Sku == "P01").Stock);
        await e.EvidenciaAsync("P33", "DEFECTO dia20 aceptado54; ARREPENTIMIENTO dia10 sin reembolso/reintegro", new { aceptada, Rechazo = ex.Message, RechazoMovimientos = d.Movimientos.Count });
    }
    [Fact] public async Task P34_VisitorDetalleYPeso()
    {
        await using var e = await Escenario.CrearAsync(); var kit = (await e.Consultas.CargarAsync()).Kits.Single(x => x.Codigo == "KIT_TELETRABAJO");
        var detalle = new DesgloseFisicoVisitor(); var peso = new PesoLogisticoVisitor(); kit.Aceptar(detalle); kit.Aceptar(peso);
        Assert.Equal(new[] { "P01", "P02", "P03" }, detalle.Unidades.Keys.Order()); Assert.All(detalle.Unidades.Values, n => Assert.Equal(1, n)); Assert.Equal(1, peso.PesoKg);
        await e.EvidenciaAsync("P34", "Visita anidada con detalle P01/P02/P03 y peso 1.00kg", new { detalle.Unidades, peso.PesoKg });
    }
}
