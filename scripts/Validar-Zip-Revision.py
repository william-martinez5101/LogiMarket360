"""Valida manifiesto, exclusiones y rutas antes de extraer una revision en tmp."""
import argparse
import datetime
import hashlib
import json
from pathlib import Path, PurePosixPath
import zipfile

parser = argparse.ArgumentParser()
parser.add_argument("zip", type=Path)
parser.add_argument("destino", type=Path)
parser.add_argument("registro", type=Path)
args = parser.parse_args()
raiz = Path(__file__).resolve().parent.parent
destino = args.destino.resolve()
if not destino.is_relative_to(raiz / "tmp") or destino == raiz / "tmp":
    raise ValueError("La extraccion debe usar un subdirectorio nuevo de tmp.")
if destino.exists() and any(destino.iterdir()):
    raise ValueError("El destino debe estar vacio para conservar archivos existentes.")
with zipfile.ZipFile(args.zip) as z:
    nombres = z.namelist()
    assert len(nombres) == len(set(nombres)), "Entradas duplicadas"
    for nombre in nombres:
        ruta = PurePosixPath(nombre)
        assert not ruta.is_absolute() and ".." not in ruta.parts and "\\" not in nombre and ":" not in nombre
        assert ruta.parts[0] == "LogiMarket360"
        assert not set(ruta.parts) & {".git", ".vs", "bin", "obj", "tmp", "artifacts", "TestResults"}
        assert not any(x in nombre.lower() for x in (".db", ".sqlite", "/.env")), "Base o configuracion local en el ZIP"
    manifiesto = json.loads(z.read("LogiMarket360/MANIFIESTO_REVISION.json"))
    esperadas = {"LogiMarket360/" + x["Archivo"] for x in manifiesto["Archivos"]}
    assert set(nombres) == esperadas | {"LogiMarket360/MANIFIESTO_REVISION.json", "LogiMarket360/HISTORIAL_COMMITS.txt"}
    for item in manifiesto["Archivos"]:
        assert hashlib.sha256(z.read("LogiMarket360/" + item["Archivo"])).hexdigest() == item["SHA256"]
    z.extractall(destino)
resultado = {"FechaUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(), "Resultado": "APROBADO",
    "ZIP": args.zip.name, "Commit": manifiesto["Commit"], "Entradas": len(nombres),
    "ArchivosConSHA256Verificado": len(manifiesto["Archivos"]), "SHA256ZIP": hashlib.sha256(args.zip.read_bytes()).hexdigest(),
    "Exclusiones": "Sin .git/.vs/bin/obj/tmp/artifacts/TestResults, bases de uso o .env", "DestinoTemporal": str(destino.relative_to(raiz))}
args.registro.parent.mkdir(parents=True, exist_ok=True)
args.registro.write_text(json.dumps(resultado, indent=2), encoding="utf-8")
print(json.dumps(resultado, indent=2))
