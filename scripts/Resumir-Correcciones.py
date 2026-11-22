"""Comprueba TRX y genera la matriz actual a partir de evidencia realmente ejecutada."""
from collections import Counter
import datetime
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET

raiz = Path(__file__).resolve().parent.parent
carpeta = raiz / "docs/evidencia/correcciones-2026-11-22"
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
def leer(nombre):
    arbol = ET.parse(carpeta / nombre)
    resultados = arbol.findall(".//t:UnitTestResult", ns)
    return resultados, dict(arbol.find(".//t:Counters", ns).attrib)
originales, base = leer("antes/originales-109.trx")
finales, total = leer("final/suite-final.trx")
adjuntas, adicional = leer("final/adjuntas-final.trx")
fallos, antes = leer("antes/adicionales-fallos.trx")
assert len(originales) == 109 and all(x.attrib["outcome"] == "Passed" for x in originales)
assert len(finales) == 149 and all(x.attrib["outcome"] == "Passed" for x in finales)
assert len(adjuntas) == 5 and all(x.attrib["outcome"] == "Passed" for x in adjuntas)
assert len(fallos) == 5 and all(x.attrib["outcome"] == "Failed" for x in fallos)
conservadas = Counter(x.attrib["testName"] for x in originales)
actuales = Counter(x.attrib["testName"] for x in finales)
assert not (conservadas - actuales), "Faltan casos originales"
filas = ["# P01-P38 despues de las correcciones", "",
    "Fuente: GUIA_APE_04.pdf, casos oficiales de las paginas 17-18. Suite final 149/149; cada metodo oficial consta aprobado en el TRX. Los valores se extraen de los JSON de la misma ejecucion, sin sustituir los registros anteriores.", "",
    "| Caso | Resultado real | Esperado | Obtenido | Evidencia / fecha UTC |", "| --- | --- | --- | --- | --- |"]
for n in range(1, 39):
    caso = f"P{n:02d}"
    pruebas = [x for x in finales if re.search(r"\." + caso + "_", x.attrib["testName"])]
    assert pruebas and all(x.attrib["outcome"] == "Passed" for x in pruebas)
    dato = json.loads((carpeta / f"aceptacion-final/{caso}.json").read_text(encoding="utf-8-sig"))
    assert dato["Caso"] == caso and dato["Estado"] == "APROBADO"
    obtenido = json.dumps(dato["Obtenido"], ensure_ascii=False, separators=(",", ":"))
    if len(obtenido) > 1600: obtenido = obtenido[:1600] + "... (registro completo enlazado)"
    def celda(s): return str(s).replace("|", "&#124;").replace("\n", " ").replace("\r", " ")
    filas.append(f"| {caso} | APROBADO | {celda(dato['Esperado'])} | {celda(obtenido)} | [{caso}.json](aceptacion-final/{caso}.json) / {dato['FechaUtc']} |")
(carpeta / "P01-P38.md").write_text("\n".join(filas) + "\n", encoding="utf-8")
resumen = {"FechaUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(), "OriginalesAntes": base,
    "OriginalesConservadas": 109, "RegresionesNuevas": 40, "SuiteFinal": total, "AdjuntasAntes": antes,
    "AdjuntasFinal": adicional, "AceptacionesAprobadas": [f"P{n:02d}" for n in range(1, 39)]}
(carpeta / "resumen-pruebas.json").write_text(json.dumps(resumen, indent=2), encoding="utf-8")
print("APROBADO: 109 originales conservadas + 40 regresiones = 149; adjuntas 5/5; P01-P38 aprobadas.")
