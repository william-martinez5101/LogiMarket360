using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiMarket.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Auditorias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Correlacion = table.Column<string>(type: "TEXT", nullable: false),
                    Mensaje = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Categoria = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                    table.CheckConstraint("CK_Cliente", "length(trim(Nombre)) > 0 AND Categoria BETWEEN 0 AND 2");
                });

            migrationBuilder.CreateTable(
                name: "Kits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Precio = table.Column<decimal>(type: "TEXT", nullable: false),
                    PesoKg = table.Column<decimal>(type: "TEXT", nullable: false),
                    Stock = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                    table.UniqueConstraint("AK_Productos_Sku", x => x.Sku);
                    table.CheckConstraint("CK_Producto_Valores", "CAST(Precio AS REAL) > 0 AND CAST(PesoKg AS REAL) >= 0 AND Stock >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Rol = table.Column<int>(type: "INTEGER", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.CheckConstraint("CK_Usuario_Rol", "Rol BETWEEN 0 AND 3");
                });

            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClienteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClienteNombre = table.Column<string>(type: "TEXT", nullable: false),
                    Categoria = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoEntrega = table.Column<int>(type: "INTEGER", nullable: false),
                    Dias = table.Column<int>(type: "INTEGER", nullable: false),
                    Guia = table.Column<string>(type: "TEXT", nullable: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    Descuento = table.Column<decimal>(type: "TEXT", nullable: false),
                    Tarifa = table.Column<decimal>(type: "TEXT", nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", nullable: false),
                    Promocion = table.Column<string>(type: "TEXT", nullable: false),
                    Unidades = table.Column<int>(type: "INTEGER", nullable: false),
                    Creado = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Entregado = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Devuelto = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                    table.CheckConstraint("CK_Pedido", "Estado BETWEEN 0 AND 5 AND Dias > 0 AND Unidades > 0 AND CAST(Total AS REAL) >= 0 AND CAST(Descuento AS REAL) >= 0 AND CAST(Tarifa AS REAL) >= 0");
                    table.ForeignKey(
                        name: "FK_Pedidos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KitComponentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    KitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SubkitId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitComponentes", x => x.Id);
                    table.CheckConstraint("CK_KitComponente", "Cantidad > 0 AND ((ProductoId IS NULL) <> (SubkitId IS NULL)) AND KitId <> coalesce(SubkitId, '')");
                    table.ForeignKey(
                        name: "FK_KitComponentes_Kits_KitId",
                        column: x => x.KitId,
                        principalTable: "Kits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KitComponentes_Kits_SubkitId",
                        column: x => x.SubkitId,
                        principalTable: "Kits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KitComponentes_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Despachos",
                columns: table => new
                {
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Guia = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Estimada = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Despachos", x => x.PedidoId);
                    table.ForeignKey(
                        name: "FK_Despachos_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Devoluciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", nullable: false),
                    Reembolso = table.Column<decimal>(type: "TEXT", nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devoluciones", x => x.Id);
                    table.CheckConstraint("CK_Devolucion", "CAST(Reembolso AS REAL) >= 0");
                    table.ForeignKey(
                        name: "FK_Devoluciones_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventosOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Importe = table.Column<decimal>(type: "TEXT", nullable: false),
                    Canal = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Intentos = table.Column<int>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true),
                    Procesado = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosOutbox_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorialEstados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Desde = table.Column<int>(type: "INTEGER", nullable: true),
                    Hasta = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialEstados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialEstados_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosInventario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Sku = table.Column<string>(type: "TEXT", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosInventario", x => x.Id);
                    table.CheckConstraint("CK_Movimiento", "Cantidad <> 0");
                    table.ForeignKey(
                        name: "FK_MovimientosInventario_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosInventario_Productos_Sku",
                        column: x => x.Sku,
                        principalTable: "Productos",
                        principalColumn: "Sku",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Clave = table.Column<string>(type: "TEXT", nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", nullable: false),
                    Importe = table.Column<decimal>(type: "TEXT", nullable: false),
                    Proveedor = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.CheckConstraint("CK_Pago", "CAST(Importe AS REAL) > 0 AND length(Clave) > 0");
                    table.ForeignKey(
                        name: "FK_Pagos_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidoDetalles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Precio = table.Column<decimal>(type: "TEXT", nullable: false),
                    ArbolJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoDetalles", x => x.Id);
                    table.CheckConstraint("CK_Detalle", "Cantidad > 0 AND CAST(Precio AS REAL) > 0");
                    table.ForeignKey(
                        name: "FK_PedidoDetalles_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntregasOutbox",
                columns: table => new
                {
                    EventoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Suscriptor = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregasOutbox", x => new { x.EventoId, x.Suscriptor });
                    table.ForeignKey(
                        name: "FK_EntregasOutbox_EventosOutbox_EventoId",
                        column: x => x.EventoId,
                        principalTable: "EventosOutbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MetricasEventos",
                columns: table => new
                {
                    EventoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Importe = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricasEventos", x => x.EventoId);
                    table.ForeignKey(
                        name: "FK_MetricasEventos_EventosOutbox_EventoId",
                        column: x => x.EventoId,
                        principalTable: "EventosOutbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    EventoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Canal = table.Column<string>(type: "TEXT", nullable: false),
                    Mensaje = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.EventoId);
                    table.ForeignKey(
                        name: "FK_Notificaciones_EventosOutbox_EventoId",
                        column: x => x.EventoId,
                        principalTable: "EventosOutbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidoDetalleComponentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DetalleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Precio = table.Column<decimal>(type: "TEXT", nullable: false),
                    PesoKg = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoDetalleComponentes", x => x.Id);
                    table.CheckConstraint("CK_Fisico", "Cantidad > 0 AND CAST(Precio AS REAL) > 0 AND CAST(PesoKg AS REAL) >= 0");
                    table.ForeignKey(
                        name: "FK_PedidoDetalleComponentes_PedidoDetalles_DetalleId",
                        column: x => x.DetalleId,
                        principalTable: "PedidoDetalles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidoDetalleComponentes_Productos_Sku",
                        column: x => x.Sku,
                        principalTable: "Productos",
                        principalColumn: "Sku",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Correlacion",
                table: "Auditorias",
                column: "Correlacion");

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Tipo",
                table: "Auditorias",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Nombre",
                table: "Clientes",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Despachos_Guia",
                table: "Despachos",
                column: "Guia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_PedidoId",
                table: "Devoluciones",
                column: "PedidoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_Referencia",
                table: "Devoluciones",
                column: "Referencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosOutbox_PedidoId_Tipo",
                table: "EventosOutbox",
                columns: new[] { "PedidoId", "Tipo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosOutbox_Procesado",
                table: "EventosOutbox",
                column: "Procesado");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstados_PedidoId_Orden",
                table: "HistorialEstados",
                columns: new[] { "PedidoId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitComponentes_KitId",
                table: "KitComponentes",
                column: "KitId");

            migrationBuilder.CreateIndex(
                name: "IX_KitComponentes_ProductoId",
                table: "KitComponentes",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_KitComponentes_SubkitId",
                table: "KitComponentes",
                column: "SubkitId");

            migrationBuilder.CreateIndex(
                name: "IX_Kits_Codigo",
                table: "Kits",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_PedidoId",
                table: "MovimientosInventario",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_Referencia_Sku_Motivo",
                table: "MovimientosInventario",
                columns: new[] { "Referencia", "Sku", "Motivo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_Sku",
                table: "MovimientosInventario",
                column: "Sku");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_Clave",
                table: "Pagos",
                column: "Clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_PedidoId",
                table: "Pagos",
                column: "PedidoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_Referencia",
                table: "Pagos",
                column: "Referencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidoDetalleComponentes_DetalleId",
                table: "PedidoDetalleComponentes",
                column: "DetalleId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoDetalleComponentes_Sku",
                table: "PedidoDetalleComponentes",
                column: "Sku");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoDetalles_PedidoId_Orden",
                table: "PedidoDetalles",
                columns: new[] { "PedidoId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_ClienteId",
                table: "Pedidos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_ClienteNombre",
                table: "Pedidos",
                column: "ClienteNombre");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_Estado",
                table: "Pedidos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Nombre",
                table: "Productos",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Nombre",
                table: "Usuarios",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Auditorias");

            migrationBuilder.DropTable(
                name: "Despachos");

            migrationBuilder.DropTable(
                name: "Devoluciones");

            migrationBuilder.DropTable(
                name: "EntregasOutbox");

            migrationBuilder.DropTable(
                name: "HistorialEstados");

            migrationBuilder.DropTable(
                name: "KitComponentes");

            migrationBuilder.DropTable(
                name: "MetricasEventos");

            migrationBuilder.DropTable(
                name: "MovimientosInventario");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "PedidoDetalleComponentes");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Kits");

            migrationBuilder.DropTable(
                name: "EventosOutbox");

            migrationBuilder.DropTable(
                name: "PedidoDetalles");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
