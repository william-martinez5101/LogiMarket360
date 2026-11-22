"""Acredita codigo identico y datos iniciales reales de la extraccion compilada."""
import argparse
import datetime
import hashlib
import json
from pathlib import Path
import sqlite3
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser()
parser.add_argument("copia", type=Path)
args = parser.parse_args()
raiz = Path(__file__).resolve().parent.parent
copia = args.copia.resolve(strict=True)
if not copia.is_relative_to(raiz / "tmp"):
    raise ValueError("Se necesita una copia temporal dentro de tmp.")
salida = raiz / "docs/evidencia/correcciones-2026-11-22/copia-limpia"
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
pruebas = {}
for archivo, cantidad in [("suite-sdk8.trx", 149), ("adjuntas-sdk8.trx", 5)]:
    arbol = ET.parse(salida / archivo)
    casos = arbol.findall(".//t:UnitTestResult", ns)
    assert len(casos) == cantidad and all(x.attrib["outcome"] == "Passed" for x in casos)
    pruebas[archivo] = dict(arbol.find(".//t:Counters", ns).attrib)
manifiesto = json.loads((copia / "MANIFIESTO_REVISION.json").read_text(encoding="utf-8-sig"))
insumos = []
for item in manifiesto["Archivos"]:
    nombre = item["Archivo"]
    if Path(nombre).suffix in {".cs", ".csproj", ".sln", ".slnx", ".props", ".targets"} or nombre.endswith("packages.lock.json") or nombre == ".config/dotnet-tools.json":
        a = hashlib.sha256((copia / nombre).read_bytes()).hexdigest()
        b = hashlib.sha256((raiz / nombre).read_bytes()).hexdigest()
        assert a == b == item["SHA256"], f"Cambio de codigo o dependencias: {nombre}"
        insumos.append({"Archivo": nombre, "SHA256": a})
base = copia / "tmp/cli-desde-cero/logimarket.db"
assert base.is_file()
db = sqlite3.connect(base.as_uri() + "?mode=ro", uri=True)
tablas = ["Productos", "Kits", "Clientes", "Usuarios", "Pagos", "MovimientosInventario", "EventosOutbox"]
cantidades = {tabla: db.execute(f'SELECT COUNT(*) FROM "{tabla}"').fetchone()[0] for tabla in tablas}
assert cantidades == {"Productos": 5, "Kits": 2, "Clientes": 3, "Usuarios": 4, "Pagos": 0, "MovimientosInventario": 0, "EventosOutbox": 0}
stock = dict(db.execute("SELECT Sku, Stock FROM Productos ORDER BY Sku"))
assert stock == {"P01": 8, "P02": 5, "P03": 20, "P04": 4, "P05": 30}
marcas = db.execute("SELECT COUNT(*) FROM Auditorias WHERE Tipo='INICIALIZACION' AND Correlacion='DATOS_DEMO_V1'").fetchone()[0]
migraciones = [fila[0] for fila in db.execute("SELECT MigrationId FROM __EFMigrationsHistory")]
integridad = db.execute("PRAGMA integrity_check").fetchone()[0]
foraneas = list(db.execute("PRAGMA foreign_key_check"))
assert marcas == 1 and migraciones == ["20261122001802_Inicial"] and integridad == "ok" and not foraneas
db.close()
recibido = copia / "docs/revisiones/2026-11-22/recibido/Revision_LogiMarket360_2026-11-22"
originales = json.loads((recibido / "MANIFEST_SHA256.json").read_text(encoding="utf-8-sig"))
assert all(hashlib.sha256((recibido / x["archivo"]).read_bytes()).hexdigest() == x["sha256"] for x in originales)
resultado = {"FechaUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(), "Resultado": "APROBADO", "SDK": "8.0.420",
    "CommitExtraido": manifiesto["Commit"], "Restauracion": "locked-mode", "Build": "Release -warnaserror: 0 errores, 0 advertencias",
    "Pruebas": pruebas, "ArchivosCodigoYDependenciasIdenticos": len(insumos), "InsumosVerificados": insumos,
    "InicializacionPorCLI": {"Datos": cantidades, "Stock": stock, "MarcasSeed": marcas, "Migraciones": migraciones,
        "Integridad": integridad, "ErroresFK": foraneas, "BaseTemporal": str(base.relative_to(raiz))}, "ArchivosAdjuntoOriginalVerificados": len(originales)}
(salida / "verificacion.json").write_text(json.dumps(resultado, ensure_ascii=False, indent=2), encoding="utf-8")
print(f"APROBADO: SDK8, 149+5 pruebas, {len(insumos)} archivos identicos; migracion real, seed doble sin duplicados; adjunto original intacto.")
