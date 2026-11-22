"""Inspeccion de solo lectura de los datos producidos por el recorrido Windows R06."""
import argparse, datetime, json, sqlite3
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("base", type=Path)
parser.add_argument("fase", choices=["pendiente", "B-rechazado", "A-recuperado", "final"])
args = parser.parse_args()
raiz = Path(__file__).resolve().parent.parent
base = args.base.resolve(strict=True)
if not base.is_relative_to(raiz / "tmp"): raise ValueError("Solo bases temporales dentro de tmp.")
db = sqlite3.connect(base.as_uri() + "?mode=ro", uri=True); db.row_factory = sqlite3.Row
tablas = ["Productos", "Pedidos", "PedidoDetalles", "PedidoDetalleComponentes", "Pagos", "MovimientosInventario", "EventosOutbox", "Notificaciones", "EntregasOutbox", "MetricasEventos", "Auditorias"]
datos = {t: [dict(f) for f in db.execute(f'SELECT * FROM "{t}"')] for t in tablas}
integridad = db.execute("PRAGMA integrity_check").fetchone()[0]; foraneas = list(db.execute("PRAGMA foreign_key_check")); db.close()
assert integridad == "ok" and not foraneas
cobros = json.loads((base.parent / "cobros-simulados.json").read_text(encoding="utf-8")); assert len(cobros) == 1 and float(cobros[0]["Monto"]) == 165
assert len(datos["Pedidos"]) == 2
a = next(p for p in datos["Pedidos"] if p["Id"].lower() == cobros[0]["PedidoId"].lower())
b = next(p for p in datos["Pedidos"] if p["Id"] != a["Id"])
assert float(a["Total"]) == float(b["Total"]) == 165 and b["Estado"] == 1
stock = next(p["Stock"] for p in datos["Productos"] if p["Sku"] == "P04")
for p in (a, b):
    detalles = [x for x in datos["PedidoDetalles"] if x["PedidoId"] == p["Id"]]
    assert len(detalles) == 1 and detalles[0]["Codigo"] == "P04" and detalles[0]["Cantidad"] == 4
if args.fase in ("pendiente", "B-rechazado"):
    assert a["Estado"] == 1 and stock == 4
    assert not datos["Pagos"] and not datos["MovimientosInventario"] and not datos["EventosOutbox"] and not datos["Notificaciones"]
else:
    assert a["Estado"] == 2 and stock == 0
    assert len(datos["Pagos"]) == len(datos["MovimientosInventario"]) == len(datos["EventosOutbox"]) == len(datos["Notificaciones"]) == len(datos["MetricasEventos"]) == 1
    assert len(datos["EntregasOutbox"]) == 3 and datos["EventosOutbox"][0]["Procesado"] is not None
    pago = datos["Pagos"][0]; movimiento = datos["MovimientosInventario"][0]
    assert pago["Referencia"] == cobros[0]["Referencia"] and pago["Proveedor"] == cobros[0]["Proveedor"] == "LEGADO"
    assert pago["PedidoId"] == a["Id"] and movimiento["PedidoId"] == a["Id"] and movimiento["Sku"] == "P04" and movimiento["Cantidad"] == -4
    previo = json.loads((raiz / "docs/evidencia/R06-2026-11-22/ui/pendiente-sqlite.json").read_text(encoding="utf-8"))
    assert previo["CobrosSimulados"] == cobros
assert all(p["Stock"] >= 0 for p in datos["Productos"])
resultado = {"FechaUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(), "Fase": args.fase, "Resultado": "APROBADO",
    "BaseTemporal": str(base.relative_to(raiz)), "PedidoA": a["Id"], "PedidoB": b["Id"], "StockP04": stock,
    "ComprometidoP04Esperado": 4 if not datos["Pagos"] else 0, "DisponibleP04Esperado": 0,
    "Integridad": integridad, "ErroresFK": foraneas, "Datos": datos, "CobrosSimulados": cobros,
    "Intentos": json.loads((base.parent / "intentos-cobro.json").read_text(encoding="utf-8"))}
destino = raiz / "docs/evidencia/R06-2026-11-22/ui"; destino.mkdir(parents=True, exist_ok=True)
(destino / f"{args.fase}-sqlite.json").write_text(json.dumps(resultado, ensure_ascii=False, indent=2), encoding="utf-8")
print(f"APROBADO {args.fase}: aceptaciones1/165, pagos{len(datos['Pagos'])}, movimientos{len(datos['MovimientosInventario'])}, stockP04={stock}, integridad=ok")
