using LogiMarket.Application.Contratos;
using LogiMarket.Application.Documentos;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Presentacion;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Ventas;
using LogiMarket.Infrastructure.Documentos;
using LogiMarket.Infrastructure.Pagos;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.Tests.Integracion;

public sealed partial class AceptacionTests
{
    [Fact] public async Task P11_FactoryMethodEmailYSms()
    {
        await using var e = await Escenario.CrearAsync();
        Assert.IsType<LogiMarket.Infrastructure.Eventos.NotificadorEmail>(e.Creadores[0].CrearNotificador());
        Assert.IsType<LogiMarket.Infrastructure.Eventos.NotificadorSms>(e.Creadores[1].CrearNotificador());
        await e.PagadoAsync(); await e.Outbox.ProcesarAsync(); e.Demo.Canal = "SMS";
        await e.PagadoAsync("Luis", TipoEntrega.ESTANDAR, ("P05", 1)); await e.Outbox.ProcesarAsync();
        var d = await e.Consultas.CargarAsync(); Assert.Equal(2, d.Avisos.Count); Assert.Contains(d.Avisos, x => x.Canal == "EMAIL" && x.Mensaje.Contains("PedidoPagado")); Assert.Contains(d.Avisos, x => x.Canal == "SMS");
        await e.EvidenciaAsync("P11", "Creadores concretos EMAIL/SMS emiten avisos de eventos reales", d.Avisos);
    }
    [Fact] public async Task P12_AbstractFactoryFamilias()
    {
        await using var e = await Escenario.CrearAsync(); var id = Guid.NewGuid(); var a = e.Entregas[0].CrearPlan(id); var b = e.Entregas[1].CrearPlan(id);
        Assert.Equal(5, a.Tarifa); Assert.Equal(3, a.Dias); Assert.Contains("ESTANDAR", a.Guia);
        Assert.Equal(12, b.Tarifa); Assert.Equal(1, b.Dias); Assert.Contains("EXPRES", b.Guia);
        Assert.Throws<InvalidOperationException>(() => new FabricaMezclada().CrearPlan(id));
        await e.EvidenciaAsync("P12", "Tarifas 5/12, plazos 3/1, guias compatibles y mezcla rechazada", new { a, b });
    }
    private sealed class FabricaMezclada : FabricaEntrega
    {
        public override TipoEntrega Tipo => TipoEntrega.ESTANDAR;
        public override ITarifaEntrega CrearTarifa() => new EntregaExpresFactory().CrearTarifa();
        public override IPlazoEntrega CrearPlazo() => new EntregaEstandarFactory().CrearPlazo();
        public override IEtiquetaEntrega CrearEtiqueta() => new EntregaEstandarFactory().CrearEtiqueta();
    }
    [Fact] public async Task P13_BuilderValidoEIncompleto()
    {
        await using var e = await Escenario.CrearAsync(); var builder = new PedidoBuilder(e.Calculadora, e.Entregas);
        Assert.Throws<InvalidOperationException>(() => builder.Construir(e.Reloj.Ahora, "test")); var d = await e.Consultas.CargarAsync();
        builder.ParaCliente(d.Clientes.Single(x => x.Nombre == "Ana")); Assert.Throws<InvalidOperationException>(() => builder.Construir(e.Reloj.Ahora, "test"));
        var p = builder.Agregar(d.Kits.Single(x => x.Codigo == "KIT_TELETRABAJO"), 1).Agregar(d.Productos.Single(x => x.Sku == "P05"), 2).Construir(e.Reloj.Ahora, "test");
        Assert.Equal(2, p.Lineas.Count); Assert.Equal(68, p.Valores.Total); await e.EvidenciaAsync("P13", "Sin cliente/lineas rechazado; agregado valido de varias lineas", new { p.Valores, Lineas = p.Lineas.Count });
    }
    [Fact] public async Task P14_PrototypeProfundoPersistido()
    {
        await using var e = await Escenario.CrearAsync(); var plantilla = await e.BorradorAsync(); var copia1 = await e.Pedidos.ClonarAsync(plantilla); var copia2 = await e.Pedidos.ClonarAsync(plantilla);
        var d = await e.Consultas.CargarAsync(); var p = d.Pedidos.Single(x => x.Id == copia2); var b = new PedidoBorrador(); b.SeleccionarCliente(p.ClienteId); b.Agregar(p.Lineas[0].Copiar(2));
        await e.Pedidos.RegistrarAsync(b, copia2, p.Version); var despues = await e.Consultas.CargarAsync();
        Assert.Equal(1, despues.Pedidos.Single(x => x.Id == plantilla).Lineas[0].Cantidad); Assert.Equal(1, despues.Pedidos.Single(x => x.Id == copia1).Lineas[0].Cantidad); Assert.Equal(2, despues.Pedidos.Single(x => x.Id == copia2).Lineas[0].Cantidad);
        Assert.NotSame(d.Pedidos[0].Lineas[0].Elemento.Hijos, d.Pedidos[1].Lineas[0].Elemento.Hijos); Assert.Empty(d.Pagos);
        await e.EvidenciaAsync("P14", "Tres identidades; editar copia2 no cambia plantilla ni copia1", despues.Pedidos.Select(x => new { x.Id, Cantidad = x.Lineas[0].Cantidad }));
    }
    [Fact] public async Task P15_SingletonMetricasThreadSafe()
    {
        await using var e = await Escenario.CrearAsync(); var servicios = new ServiceCollection().AddSingleton<MetricasSesion>();
        using var proveedor = servicios.BuildServiceProvider(); var panel = proveedor.GetRequiredService<MetricasSesion>(); var auditoria = proveedor.GetRequiredService<MetricasSesion>(); Assert.Same(panel, auditoria);
        var metricas = Enumerable.Range(0, 1000).Select(_ => new MetricaRegistrada(Guid.NewGuid(), "PedidoPagado", 1)).ToArray();
        Parallel.For(0, 10, _ => panel.Sincronizar(metricas)); Assert.Equal(1000, auditoria.Pagos); Assert.Equal(1000, auditoria.Ventas);
        await e.EvidenciaAsync("P15", "Misma instancia DI; 1000 ids aplicados por 10 hilos sin duplicar", new { MismaInstancia = ReferenceEquals(panel, auditoria), panel.Pagos, panel.Ventas });
    }
    [Fact] public async Task P16_AdapterYAlternativoSustituibles()
    {
        await using var e = await Escenario.CrearAsync(); await e.PagadoAsync(); e.Demo.Proveedor = "ALTERNATIVO"; await e.PagadoAsync("Luis", TipoEntrega.ESTANDAR, ("P05", 1));
        var d = await e.Consultas.CargarAsync(); Assert.Contains(d.Pagos, p => p.Proveedor == "LEGADO"); Assert.Contains(d.Pagos, p => p.Proveedor == "ALTERNATIVO");
        foreach (var tipo in new[] { "LEGADO", "ALTERNATIVO" })
        { e.Demo.Proveedor = tipo; await Assert.ThrowsAsync<ArgumentException>(() => e.Pasarela.CobrarAsync(-1, "invalido")); }
        Assert.Equal(2, e.Diario.Leer().Count); await e.EvidenciaAsync("P16", "Ambos proveedores ejecutan mismo caso de uso y contrato de errores", d.Pagos);
    }
    private sealed class SalidaCaptura : ISalidaDocumento
    {
        public string Contenido { get; private set; } = "";
        public Task<DocumentoEmitido> EmitirAsync(string nombre, string contenido) { Contenido = contenido; return Task.FromResult(new DocumentoEmitido(contenido, null)); }
    }
    [Fact] public async Task P17_BridgeCuatroCombinaciones()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.EntregadoAsync(); var dev = await e.Devoluciones.ProcesarAsync(new(id, "DEFECTO", true)); var p = (await e.Consultas.CargarAsync()).Pedidos.Single();
        var vista = new SalidaCaptura(); var archivo = new ArchivoTxt(Path.Combine(e.Config.Directorio, "comprobantes"));
        var v1 = await new ComprobanteVenta(vista).EmitirAsync(p); var v2 = await new ComprobanteDevolucion(vista).EmitirAsync(p, dev);
        var t1 = await new ComprobanteVenta(archivo).EmitirAsync(p); var t2 = await new ComprobanteDevolucion(archivo).EmitirAsync(p, dev);
        Assert.Contains("59.00", v1.Contenido); Assert.Contains("54.00", v2.Contenido); Assert.Equal(t1.Contenido, await File.ReadAllTextAsync(t1.Ruta!)); Assert.Equal(t2.Contenido, await File.ReadAllTextAsync(t2.Ruta!));
        await e.EvidenciaAsync("P17", "Venta/devolucion por salida observable y dos TXT reales; la vista Windows se verifica en recorrido UI", new { Venta = t1.Contenido, Devolucion = t2.Contenido, Archivos = new[] { t1.Ruta, t2.Ruta } });
    }
    [Fact] public async Task P18_CompositeYCiclosPersistidos()
    {
        await using var e = await Escenario.CrearAsync(); var d = await e.Consultas.CargarAsync(); var kit = d.Kits.Single(x => x.Codigo == "KIT_TELETRABAJO");
        Assert.Equal(50, d.Kits.Single(x => x.Codigo == "KIT_OFICINA").CalcularPrecio()); Assert.Equal(60, kit.CalcularPrecio());
        var v = new DesgloseFisicoVisitor(); kit.Aceptar(v); Assert.Equal(3, v.Unidades.Count);
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Catalogo.GuardarKitAsync("KIT_OFICINA", "Ciclo", [new("KIT_TELETRABAJO", 1)]));
        Assert.Equal(50, (await e.Consultas.CargarAsync()).Kits.Single(x => x.Codigo == "KIT_OFICINA").CalcularPrecio());
        await e.EvidenciaAsync("P18", "Precios 50/60; expansion P01/P02/P03; ciclo sin cambios", v.Unidades);
    }
    [Fact] public async Task P19_DecoratorOrdenYResultado()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.ValidadoAsync(); await e.Facade.ConfirmarAsync(id, "decorador-exito");
        Assert.Equal(new[] { "PAGO_INICIO", "PASARELA", "PAGO_FIN" }, e.Log.Leer().Where(x => x.Correlacion == "decorador-exito").Select(x => x.Tipo));
        var id2 = await e.ValidadoAsync("Luis", TipoEntrega.ESTANDAR, ("P05", 1)); e.Demo.FallarCobro = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Facade.ConfirmarAsync(id2, "decorador-fallo"));
        Assert.Equal(new[] { "PAGO_INICIO", "PASARELA", "PAGO_ERROR" }, e.Log.Leer().Where(x => x.Correlacion == "decorador-fallo").Select(x => x.Tipo));
        Assert.Single((await e.Consultas.CargarAsync()).Pagos); await e.EvidenciaAsync("P19", "INICIO->PASARELA->FIN/ERROR; resultado preservado", e.Log.Leer());
    }
    [Fact] public async Task P20_FacadeRollbackYRecuperacionIdempotente()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.ValidadoAsync(); e.Demo.FallarPersistencia = true;
        await Assert.ThrowsAsync<IOException>(() => e.Facade.ConfirmarAsync(id, "rollback")); var antes = await e.Consultas.CargarAsync();
        Assert.Empty(antes.Pagos); Assert.Empty(antes.Movimientos); Assert.Empty(antes.Outbox); Assert.Equal(EstadoPedido.VALIDADO, antes.Pedidos.Single().Estado); Assert.Single(e.Diario.Leer());
        e.Demo.FallarPersistencia = false; await e.Facade.ConfirmarAsync(id, "rollback"); Assert.Single(e.Diario.Leer()); Assert.Single((await e.Consultas.CargarAsync()).Pagos);
        await e.EvidenciaAsync("P20", "Fallo antes de commit sin persistencia parcial; reintento recupera cobro original", new { PagoLocalAntes = antes.Pagos.Count, CobrosProveedorFinales = e.Diario.Leer().Count });
    }
    [Fact] public async Task P21_FlyweightMilFilasMedicionEquivalente()
    {
        await using var e = await Escenario.CrearAsync(); var d = await e.Consultas.CargarAsync(); var catalogo = d.Productos.Select(p => new MetadatosCatalogo(p.Sku, p.Nombre, "Accesorios", "Accesorio de computacion")).ToArray();
        var r = MedicionFlyweight.Ejecutar(catalogo); Assert.Equal(1000, r.Filas); Assert.Equal(5, r.ObjetosCompartidos); Assert.Equal(1000, r.ObjetosSinCompartir); Assert.True(r.IdentidadCompartida); Assert.True(r.BytesCompartidos > 0); Assert.True(r.BytesSinCompartir > 0);
        await e.EvidenciaAsync("P21", "1000 filas identicas en ambos escenarios; 5 vs 1000 metadatos; bytes asignados del mismo hilo con calentamiento", r);
    }
    [Fact] public async Task P22_ProxyDeniegaAntesDeLogYCobro()
    {
        await using var e = await Escenario.CrearAsync(); var id = await e.ValidadoAsync(); var inicial = e.Log.Leer().Count;
        foreach (var rol in new[] { Rol.CONSULTA, Rol.BODEGA })
        { await e.ComoAsync(rol); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => e.Pasarela.CobrarAsync(59, "denegado")); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => e.Facade.ConfirmarAsync(id, "denegado")); }
        Assert.Equal(inicial, e.Log.Leer().Count); Assert.Empty(e.Diario.Leer()); var d = await e.Consultas.CargarAsync(); Assert.Empty(d.Pagos); Assert.Empty(d.Outbox);
        await e.EvidenciaAsync("P22", "CONSULTA/BODEGA: 0 log de pago, 0 proveedor, 0 efectos", new { Antes = inicial, Despues = e.Log.Leer().Count, Cobros = e.Diario.Leer().Count });
    }
}
