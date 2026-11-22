"""Verifica conservacion de casos y genera evidencia resumida R06 desde TRX/JSON reales."""
from collections import Counter
import datetime, hashlib, json, re
from pathlib import Path
import xml.etree.ElementTree as ET

raiz = Path(__file__).resolve().parent.parent
carpeta = raiz / "docs/evidencia/R06-2026-11-22"
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
def leer(ruta, cantidad, resultado="Passed"):
    arbol = ET.parse(ruta); casos = arbol.findall(".//t:UnitTestResult", ns)
    assert len(casos) == cantidad and all(x.attrib["outcome"] == resultado for x in casos)
    return Counter(x.attrib["testName"] for x in casos), dict(arbol.find(".//t:Counters", ns).attrib)
base, contadores_base = leer(carpeta / "antes/149-originales.trx", 149)
actual, contadores_actual = leer(carpeta / "final/suite-final.trx", 163)
assert not (base - actual)
_, contadores_cinco = leer(carpeta / "final/cinco-anteriores.trx", 5)
_, contadores_nueva = leer(carpeta / "despues/R06-adjunta-despues.trx", 1)
_, contadores_fallo = leer(carpeta / "antes/R06-adjunta-fallo.trx", 1, "Failed")
for recibida in (raiz / "docs/revisiones").rglob("MANIFEST_SHA256.json"):
    for item in json.loads(recibida.read_text(encoding="utf-8-sig")):
        assert hashlib.sha256((recibida.parent / item["archivo"]).read_bytes()).hexdigest() == item["sha256"]
original = raiz / "docs/revisiones/segunda-2026-11-22/recibido/Segunda_Revision_LogiMarket360_2026-11-22/prueba_nueva/CobroPendienteStockTests.cs"
copia = raiz / "LogiMarket.Tests/Integracion/RevisionAdjuntaR06Tests.cs"
def normalizar(texto): return texto.lstrip("\ufeff").replace("\r\n", "\n").strip()
assert normalizar(original.read_text(encoding="utf-8-sig")).replace("namespace RevisionStockPendiente;", "namespace LogiMarket.Tests.Integracion.RevisionR06;") == normalizar(copia.read_text(encoding="utf-8-sig"))
filas = ["# P01-P38 despues de R06", "", "Suite actual163/163; cada caso oficial figura aprobado en el TRX. Valores extraidos de los JSON de esta ejecucion, con disponibilidad fisica/comprometida/libre adicional en los registros completos.", "", "| Caso | Resultado | Esperado | Obtenido real | Evidencia / fecha UTC |", "| --- | --- | --- | --- | --- |"]
for n in range(1, 39):
    caso = f"P{n:02d}"; assert any(re.search(r"\." + caso + "_", nombre) for nombre in actual)
    dato = json.loads((carpeta / f"aceptacion-final/{caso}.json").read_text(encoding="utf-8-sig")); assert dato["Estado"] == "APROBADO"
    obtenido = json.dumps(dato["Obtenido"], ensure_ascii=False, separators=(",", ":"))
    if len(obtenido) > 1400: obtenido = obtenido[:1400] + "... (ver registro completo)"
    def celda(s): return str(s).replace("|", "&#124;").replace("\n", " ").replace("\r", " ")
    filas.append(f"| {caso} | APROBADO | {celda(dato['Esperado'])} | {celda(obtenido)} | [{caso}.json](aceptacion-final/{caso}.json) / {dato['FechaUtc']} |")
(carpeta / "P01-P38.md").write_text("\n".join(filas) + "\n", encoding="utf-8")
fases = []
for fase in ("pendiente", "B-rechazado", "A-recuperado", "final"):
    dato = json.loads((carpeta / f"ui/{fase}-sqlite.json").read_text(encoding="utf-8-sig")); assert dato["Resultado"] == "APROBADO"
    fases.append({"Fase": fase, "PedidoA": dato["PedidoA"], "PedidoB": dato["PedidoB"], "Pagos": len(dato["Datos"]["Pagos"]), "Cobros": len(dato["CobrosSimulados"]), "StockP04": dato["StockP04"]})
resumen = {"FechaUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(), "OriginalesConservadas": 149,
    "CasosR06Agregados": sum((actual - base).values()), "SuiteAntes": contadores_base, "SuiteFinal": contadores_actual,
    "CincoExternasAnteriores": contadores_cinco, "ExternaR06Antes": contadores_fallo, "ExternaR06Despues": contadores_nueva,
    "MetodosAgregados": list((actual - base).elements()), "CopiaPruebaAdjunta": "Mismas aserciones/cuerpo; solo namespace distinto para evitar colision con el proyecto externo",
    "AceptacionesAprobadas": [f"P{n:02d}" for n in range(1, 39)], "WindowsReal": fases}
(carpeta / "resumen.json").write_text(json.dumps(resumen, ensure_ascii=False, indent=2), encoding="utf-8")
print("APROBADO: 149 conservadas +14 R06 =163; externas5+1; P01-P38; cuatro fases Windows verificadas.")
