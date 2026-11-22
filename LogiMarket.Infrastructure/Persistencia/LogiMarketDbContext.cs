using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LogiMarket.Infrastructure.Persistencia;

public sealed class LogiMarketDbContext(DbContextOptions<LogiMarketDbContext> opciones) : DbContext(opciones)
{
    protected override void OnModelCreating(ModelBuilder m)
    {
        var p = m.Entity<ProductoSimple>(); p.ToTable("Productos", t => {
            t.HasCheckConstraint("CK_Producto_Valores", "CAST(Precio AS REAL) > 0 AND CAST(PesoKg AS REAL) >= 0 AND Stock >= 0"); });
        p.HasKey(x => x.Id); p.HasAlternateKey(x => x.Sku); p.HasIndex(x => x.Nombre); p.Ignore(x => x.Codigo);
        p.Property(x => x.Sku).HasMaxLength(60); p.Property(x => x.Nombre).HasMaxLength(180);
        p.Property(x => x.Stock).IsConcurrencyToken();
        var c = m.Entity<Cliente>(); c.ToTable("Clientes", t => t.HasCheckConstraint("CK_Cliente", "length(trim(Nombre)) > 0 AND Categoria BETWEEN 0 AND 2"));
        c.HasKey(x => x.Id); c.HasIndex(x => x.Nombre);
        var u = m.Entity<UsuarioFila>(); u.ToTable("Usuarios", t => t.HasCheckConstraint("CK_Usuario_Rol", "Rol BETWEEN 0 AND 3"));
        u.HasKey(x => x.Id); u.HasIndex(x => x.Nombre).IsUnique();
        var k = m.Entity<KitFila>(); k.ToTable("Kits"); k.HasKey(x => x.Id); k.HasIndex(x => x.Codigo).IsUnique();
        k.HasMany(x => x.Componentes).WithOne().HasForeignKey(x => x.KitId).OnDelete(DeleteBehavior.Cascade);
        var kc = m.Entity<KitComponenteFila>(); kc.ToTable("KitComponentes", t => {
            t.HasCheckConstraint("CK_KitComponente", "Cantidad > 0 AND ((ProductoId IS NULL) <> (SubkitId IS NULL)) AND KitId <> coalesce(SubkitId, '')"); });
        kc.HasKey(x => x.Id); kc.HasOne<ProductoSimple>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        kc.HasOne<KitFila>().WithMany().HasForeignKey(x => x.SubkitId).OnDelete(DeleteBehavior.Restrict);
        var o = m.Entity<PedidoFila>(); o.ToTable("Pedidos", t => {
            t.HasCheckConstraint("CK_Pedido", "Estado BETWEEN 0 AND 5 AND Dias > 0 AND Unidades > 0 AND CAST(Total AS REAL) >= 0 AND CAST(Descuento AS REAL) >= 0 AND CAST(Tarifa AS REAL) >= 0"); });
        o.HasKey(x => x.Id); o.HasIndex(x => x.Estado); o.HasIndex(x => x.ClienteNombre); o.Property(x => x.Version).IsConcurrencyToken();
        o.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        o.HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Cascade);
        o.HasMany(x => x.Historial).WithOne().HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Cascade);
        var d = m.Entity<DetalleFila>(); d.ToTable("PedidoDetalles", t => t.HasCheckConstraint("CK_Detalle", "Cantidad > 0 AND CAST(Precio AS REAL) > 0"));
        d.HasKey(x => x.Id); d.HasIndex(x => new { x.PedidoId, x.Orden }).IsUnique();
        d.HasMany(x => x.Fisicos).WithOne().HasForeignKey(x => x.DetalleId).OnDelete(DeleteBehavior.Cascade);
        var f = m.Entity<FisicoFila>(); f.ToTable("PedidoDetalleComponentes", t => t.HasCheckConstraint("CK_Fisico", "Cantidad > 0 AND CAST(Precio AS REAL) > 0 AND CAST(PesoKg AS REAL) >= 0"));
        f.HasKey(x => x.Id); f.HasOne<ProductoSimple>().WithMany().HasForeignKey(x => x.Sku).HasPrincipalKey(x => x.Sku).OnDelete(DeleteBehavior.Restrict);
        var h = m.Entity<EstadoFila>(); h.ToTable("HistorialEstados"); h.HasKey(x => x.Id); h.HasIndex(x => new { x.PedidoId, x.Orden }).IsUnique();
        var pago = m.Entity<PagoFila>(); pago.ToTable("Pagos", t => t.HasCheckConstraint("CK_Pago", "CAST(Importe AS REAL) > 0 AND length(Clave) > 0"));
        pago.HasKey(x => x.Id); pago.HasIndex(x => x.PedidoId).IsUnique(); pago.HasIndex(x => x.Clave).IsUnique(); pago.HasIndex(x => x.Referencia).IsUnique();
        pago.HasOne<PedidoFila>().WithOne().HasForeignKey<PagoFila>(x => x.PedidoId).OnDelete(DeleteBehavior.Restrict);
        var mov = m.Entity<MovimientoFila>(); mov.ToTable("MovimientosInventario", t => t.HasCheckConstraint("CK_Movimiento", "Cantidad <> 0"));
        mov.HasKey(x => x.Id); mov.HasIndex(x => new { x.Referencia, x.Sku, x.Motivo }).IsUnique();
        mov.HasOne<ProductoSimple>().WithMany().HasForeignKey(x => x.Sku).HasPrincipalKey(x => x.Sku).OnDelete(DeleteBehavior.Restrict);
        mov.HasOne<PedidoFila>().WithMany().HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Restrict);
        var desp = m.Entity<DespachoFila>(); desp.ToTable("Despachos"); desp.HasKey(x => x.PedidoId); desp.HasIndex(x => x.Guia).IsUnique();
        desp.HasOne<PedidoFila>().WithOne().HasForeignKey<DespachoFila>(x => x.PedidoId).OnDelete(DeleteBehavior.Restrict);
        var dev = m.Entity<DevolucionFila>(); dev.ToTable("Devoluciones", t => t.HasCheckConstraint("CK_Devolucion", "CAST(Reembolso AS REAL) >= 0"));
        dev.HasKey(x => x.Id); dev.HasIndex(x => x.PedidoId).IsUnique(); dev.HasIndex(x => x.Referencia).IsUnique();
        dev.HasOne<PedidoFila>().WithOne().HasForeignKey<DevolucionFila>(x => x.PedidoId).OnDelete(DeleteBehavior.Restrict);
        var ev = m.Entity<EventoFila>(); ev.ToTable("EventosOutbox"); ev.HasKey(x => x.Id); ev.HasIndex(x => x.Procesado);
        ev.HasIndex(x => new { x.PedidoId, x.Tipo }).IsUnique();
        ev.HasOne<PedidoFila>().WithMany().HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Restrict);
        var ent = m.Entity<EntregaEventoFila>(); ent.ToTable("EntregasOutbox"); ent.HasKey(x => new { x.EventoId, x.Suscriptor });
        ent.HasOne<EventoFila>().WithMany().HasForeignKey(x => x.EventoId).OnDelete(DeleteBehavior.Restrict);
        var aviso = m.Entity<AvisoFila>(); aviso.ToTable("Notificaciones"); aviso.HasKey(x => x.EventoId);
        aviso.HasOne<EventoFila>().WithOne().HasForeignKey<AvisoFila>(x => x.EventoId).OnDelete(DeleteBehavior.Restrict);
        var met = m.Entity<MetricaFila>(); met.ToTable("MetricasEventos"); met.HasKey(x => x.EventoId);
        met.HasOne<EventoFila>().WithOne().HasForeignKey<MetricaFila>(x => x.EventoId).OnDelete(DeleteBehavior.Restrict);
        var aud = m.Entity<BitacoraFila>(); aud.ToTable("Auditorias"); aud.HasKey(x => x.Id); aud.HasIndex(x => x.Correlacion); aud.HasIndex(x => x.Tipo);
        // Todas las identidades las asigna el agregado/repositorio. EF debe insertar hijos nuevos con Guid no vacio.
        foreach (var entidad in m.Model.GetEntityTypes())
            foreach (var propiedad in entidad.FindPrimaryKey()!.Properties)
                if (propiedad.ClrType == typeof(Guid)) propiedad.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
    }
}

public sealed record ConfiguracionBase(string Ruta)
{
    public string Directorio => Path.GetDirectoryName(Path.GetFullPath(Ruta))!;
    public static ConfiguracionBase Predeterminada() => new(Environment.GetEnvironmentVariable("LOGIMARKET_DB")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LogiMarket360", "logimarket.db"));
    public LogiMarketDbContext CrearContexto()
    {
        Directory.CreateDirectory(Directorio);
        return new(new DbContextOptionsBuilder<LogiMarketDbContext>().UseSqlite($"Data Source={Path.GetFullPath(Ruta)};Foreign Keys=True;Default Timeout=30;Pooling=False").Options);
    }
}
public sealed class FabricaDisenoContexto : IDesignTimeDbContextFactory<LogiMarketDbContext>
{
    public LogiMarketDbContext CreateDbContext(string[] args) => ConfiguracionBase.Predeterminada().CrearContexto();
}
