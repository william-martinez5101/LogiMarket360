using LogiMarket.Application.Consultas;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Presentacion;
using LogiMarket.Domain.Ventas;

namespace LogiMarket.WinForms.Vistas;

public sealed class PanelVista : VistaBase
{
    public override string Nombre => "Panel general";
    private readonly Label _indicadores = Ui.Texto("", 14, true);
    private readonly DataGridView _pedidos = Ui.Tabla("Pedidos recientes");
    private readonly Label _alertas = Ui.Texto(""); private readonly MetricasSesion _metricas;
    public PanelVista(FormularioPrincipalMediator mediador, MetricasSesion metricas) : base(mediador, "Panel general", "Resumen de operaciones, inventario y eventos pendientes.")
    {
        _metricas = metricas; var pie = Ui.Fila(_alertas); pie.Dock = DockStyle.Bottom;
        Contenido.Controls.Add(_pedidos); Contenido.Controls.Add(pie); Contenido.Controls.Add(Ui.Fila(_indicadores));
    }
    protected override void Mostrar(EstadoConsultas d)
    {
        _indicadores.Text = $"Pedidos activos: {d.Pedidos.Count(p => p.Estado is not (EstadoPedido.CANCELADO or EstadoPedido.ENTREGADO))}     Por despachar: {d.Pedidos.Count(p => p.Estado == EstadoPedido.PAGADO)}     Ventas: USD {_metricas.Ventas:N2}     Reembolsos: USD {_metricas.Reembolsos:N2}";
        Ui.Cargar(_pedidos, d.Pedidos.Take(15).Select(p => new { p.Id, Pedido = p.Id.ToString("N")[..8], Cliente = p.ClienteNombre, p.Estado, Total = p.Valores.Total, Fecha = p.Creado.ToLocalTime() }));
        _alertas.Text = $"Alertas: {d.Existencias.Count(p => p.Disponible <= 3)} productos con disponible bajo | {d.Outbox.Count(e => e.Procesado is null)} eventos Outbox pendientes | Avisos locales: {d.Avisos.Count}";
    }
}
