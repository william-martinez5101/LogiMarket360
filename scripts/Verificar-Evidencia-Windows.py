"""Inspeccion de solo lectura de la base temporal utilizada por la interfaz real."""
import argparse
import datetime
import hashlib
import json
from pathlib import Path
import sqlite3

parser = argparse.ArgumentParser()
parser.add_argument("base", type=Path)
parser.add_argument("fase", choices=["fallo", "recuperado", "reabierto"])
parser.add_argument("destino", type=Path)
args = parser.parse_args()
raiz = Path(__file__).resolve().parent.parent
base = args.base.resolve(strict=True)
if not base.is_relative_to(raiz / "tmp"):
    raise ValueError("Solo se inspeccionan bases temporales dentro de tmp del proyecto.")
db = sqlite3.connect(base.as_uri() + "?mode=ro", uri=True)
db.row_factory = sqlite3.Row
tablas = ["Pedidos", "PedidoDetalles", "Productos", "Pagos", "MovimientosInventario", "EventosOutbox", "Notificaciones", "EntregasOutbox", "MetricasEventos", "Auditorias"]
datos = {tabla: [dict(fila) for fila in db.execute(f'SELECT * FROM "{tabla}"')] for tabla in tablas}
integridad = db.execute("PRAGMA integrity_check").fetchone()[0]
foraneas = list(db.execute("PRAGMA foreign_key_check"))
db.close()
cobros = json.loads((base.parent / "cobros-simulados.json").read_text(encoding="utf-8"))
intentos = json.loads((base.parent / "intentos-cobro.json").read_text(encoding="utf-8"))
assert integridad == "ok" and not foraneas
assert len(cobros) == 1 and float(cobros[0]["Monto"]) == 59
pedido = next(p for p in datos["Pedidos"] if p["Id"].lower() == cobros[0]["PedidoId"].lower())
assert float(pedido["Total"]) == 59
carrito = next(p for p in datos["Pedidos"] if float(p["Total"]) == 77)
lineas = sorted((x for x in datos["PedidoDetalles"] if x["PedidoId"] == carrito["Id"]), key=lambda x: x["Orden"])
assert [(x["Codigo"], x["Cantidad"]) for x in lineas] == [("P04", 1), ("P01", 2)]
if args.fase == "fallo":
    assert pedido["Estado"] == 1
    assert not datos["Pagos"] and not datos["MovimientosInventario"] and not datos["EventosOutbox"] and not datos["Notificaciones"]
    assert [p["Stock"] for p in sorted(datos["Productos"], key=lambda x: x["Sku"])] == [8, 5, 20, 4, 30]
    assert any(a["Tipo"] == "COMMAND" and "ERROR" in a["Mensaje"] for a in datos["Auditorias"])
else:
    assert pedido["Estado"] == 2
    assert len(datos["Pagos"]) == 1 and len(datos["MovimientosInventario"]) == 3
    assert len(datos["EventosOutbox"]) == len(datos["Notificaciones"]) == len(datos["MetricasEventos"]) == 1
    assert len(datos["EntregasOutbox"]) == 3
    assert datos["EventosOutbox"][0]["Procesado"] is not None
    assert [p["Stock"] for p in sorted(datos["Productos"], key=lambda x: x["Sku"])] == [7, 4, 19, 4, 30]
    pago = datos["Pagos"][0]
    assert pago["Referencia"] == cobros[0]["Referencia"] and pago["Proveedor"] == cobros[0]["Proveedor"] == "LEGADO"
    assert pago["Clave"] == "UI-REINTENTO-B"
    fallo = json.loads((args.destino / "R01-fallo-sqlite.json").read_text(encoding="utf-8"))
    assert fallo["CobrosSimulados"] == cobros
resultado = {"Fase": args.fase, "Resultado": "APROBADO", "FechaUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "BaseTemporal": str(base.relative_to(raiz)), "Integridad": integridad, "ErroresFK": foraneas,
    "Datos": datos, "CobrosSimulados": cobros, "IntentosDurables": intentos,
    "SHA256Base": hashlib.sha256(base.read_bytes()).hexdigest()}
args.destino.mkdir(parents=True, exist_ok=True)
(args.destino / f"R01-{args.fase}-sqlite.json").write_text(json.dumps(resultado, ensure_ascii=False, indent=2), encoding="utf-8")
print(f"APROBADO {args.fase}: cobros={len(cobros)}, pagos={len(datos['Pagos'])}, movimientos={len(datos['MovimientosInventario'])}, avisos={len(datos['Notificaciones'])}; integridad=ok")
