# Correccion R06: disponibilidad del cobro pendiente

22/11/2026. Se trabajo sobre `codex/completar-logimarket360`, desde el HEAD comprobado `b2754b0013e000bd8e89d8bc9616d96a15c6e31b`, con arbol limpio y veinte commits existentes. Se conserva ese historial, las correcciones R01-R05 y la base de uso. La autorizacion procede de la peticion del usuario; los documentos adjuntos describen el caso y sus criterios de comprobacion.

Se descomprimio la segunda revision en `docs/revisiones/segunda-2026-11-22/recibido`, se leyeron LEEME y Prompt y se verificaron sus quince archivos originales por SHA-256. El adjunto y su prueba permanecen intactos. El ZIP recibido tiene SHA-256 `f2ac42f58ae0d82ab7654c56213aca6eb3793797af7c05a0969770316d24b24f`.

## Fallo reproducido antes de corregir

Las 149 pruebas existentes pasaron. La nueva prueba externa fallo: A aceptado por 165.00 con rollback; B aprobado por 165.00 y consumo de cuatro P04; tras reiniciar, A seguia VALIDADO y su recuperacion fallaba por STOCK disponible cero. El diario contenia dos aceptaciones por 330.00 y SQLite un solo pago. Se conserva el [TRX del fallo externo](antes/R06-adjunta-fallo.trx) y el [baseline de 149](antes/149-originales.trx).

Las primeras doce regresiones nuevas dieron nueve fallos y tres aprobaciones. Dos pruebas adicionales reprodujeron el error de lectura del diario cuando otra instancia mantenia brevemente su bloqueo. Ambos resultados anteriores se conservan en [regresiones iniciales](antes/R06-regresiones-antes.trx) y [bloqueo inicial](antes/diario-bloqueo-antes.trx).

## Solucion comprobada

La aceptacion durable ligada a PedidoId y las lineas inmutables ya persistidas del pedido VALIDADO constituyen una reserva equivalente. El diario sobrevive al rollback comercial y al cierre del proceso. Application calcula por SKU las cantidades comprometidas por aceptaciones sin pago local, expandiendo kits y sumando lineas repetidas desde las instantaneas historicas. No consulta la composicion actual para reconstruir una venta antigua.

Las otras ventas y los ajustes negativos solo consumen unidades libres. El propietario excluye exclusivamente su propio compromiso al comprobar disponibilidad y sigue validando el stock fisico. Con A por cuatro P04, quedan fisico 4 / comprometido 4 / disponible 0; B se rechaza antes del proveedor. Con A por dos, las otras dos unidades y los demas SKU siguen vendiendose.

La comprobacion y la llamada al proveedor permanecen dentro de BEGIN IMMEDIATE: dos instancias se serializan sobre la misma SQLite. Un rechazo no crea aceptacion ni compromiso. Si falla el commit, se conserva la reserva; cambiar clave o proveedor recupera la referencia original. El commit de recuperacion registra pago, descuento fisico, movimientos, estado y evento juntos; la presencia del pago consume el compromiso sin una liberacion posterior vulnerable a fallos. Outbox mantiene la deduplicacion por evento/suscriptor. Las lecturas concurrentes del diario esperan su bloqueo durante un plazo acotado y admiten cancelacion.

No se omite la validacion de stock, no se introduce stock negativo y no se cambia la migracion original. Domain sigue independiente de EF/SQLite; Infrastructure conserva los archivos de aceptacion y Application depende de sus contratos. Inventario, Catalogo y Panel presentan unidades fisicas, comprometidas y libres. [Diseno y limites completos](../../RESERVAS_EXISTENCIAS.md).

## Pruebas ejecutadas

| Comprobacion real | Resultado | Registro |
| --- | --- | --- |
| Baseline original | 149/149 aprobadas | [TRX anterior](antes/149-originales.trx) |
| Externa R06 antes | 1/1 fallida, doble aceptacion de 330.00 | [TRX del fallo](antes/R06-adjunta-fallo.trx) |
| Regresiones R06 y casos R01 relacionados | 23/23 aprobadas | [TRX enfocado](despues/R06-regresiones-despues.trx) |
| Externa R06 despues, sin modificarla | 1/1 aprobada | [TRX externo](despues/R06-adjunta-despues.trx) |
| Suite final | 163/163 aprobadas, ninguna omitida | [TRX completo](final/suite-final.trx) |
| Cinco externas de la primera revision | 5/5 aprobadas | [TRX de las cinco](final/cinco-anteriores.trx) |
| Aceptaciones del PDF | P01-P38 aprobadas nuevamente | [Matriz con valores reales](P01-P38.md) |
| Restore bloqueado y build Release -warnaserror | 0 errores, 0 advertencias | [Restore](final/restore.log), [build](despues/build-winforms.log) |

Las 149 anteriores se compararon por nombre y multiplicidad con las 163 finales; no se elimino ninguna. Los catorce casos agregados cubren ambos proveedores, rollback reiterado, otra clave/proveedor, cancelacion segura, rechazo y posterior aceptacion, unidades libres, ajustes negativos, kits y SKU repetidos, cambio posterior del catalogo, dos instancias competidoras, dos aceptaciones nuevas concurrentes, reinicio, bloqueo/cancelacion de lectura y Outbox fallido/reprocesado sin duplicados.

La prueba adjunta tambien se incorporo a `LogiMarket.Tests/Integracion/RevisionAdjuntaR06Tests.cs`, cambiando solo el namespace para evitar colisiones con el proyecto externo. El script [Resumir-R06.py](../../../scripts/Resumir-R06.py) verifica esa igualdad, las huellas de ambos adjuntos, la conservacion de los casos anteriores y los resultados reales; genera [resumen.json](resumen.json). Los JSON detallados de cada regresion estan en [regresiones](regresiones).

## Interfaz real de Windows

Se publico y ejecuto WinForms sobre una base nueva y aislada en `tmp/R06-Windows/datos/logimarket.db`. Se usaron dos procesos reales contra esa misma base; se cerro y volvio a abrir el primero antes de recuperar A. Las capturas tienen su estado accesible JSON correspondiente. Los [procesos simultaneos](ui/procesos-dos-instancias.json) y los [procesos tras reiniciar](ui/procesos-tras-reinicio.json) documentan ejecutable, argumentos e identificadores.

| Paso observado | Resultado real | Captura / datos |
| --- | --- | --- |
| Crear y validar A y B de Luis NORMAL, P04 x4 ESTANDAR | 165.00 por pedido, ambos VALIDADO | [A](ui/04-A-validado.png), [B](ui/05-B-validado.png) |
| Activar FallarPersistencia y confirmar A | Aceptacion 165.00; error local y rollback; sin pago/movimiento/evento | [Error](ui/07-A-aceptado-rollback.png), [SQLite/diario](ui/pendiente-sqlite.json) |
| Consultar inventario tras el rollback | P04 fisico 4, comprometido 4, disponible 0; otros SKU libres | [Inventario](ui/08-comprometido4-disponible0.png) |
| Confirmar B desde la segunda instancia, sin fallo | STOCK requiere 4, disponible 0; no segunda aceptacion | [Rechazo](ui/11-B-rechazado-disponible0.png), [SQLite/diario](ui/B-rechazado-sqlite.json) |
| Cerrar la primera instancia y volver a entrar | A y B conservados VALIDADO y el compromiso de A vigente | [Reinicio](ui/12-reinicio-A-pendiente.png), [pedidos](ui/13-pendiente-conservado-tras-reinicio.png) |
| Reintentar A con otra clave y proveedor ALTERNATIVO | A PAGADO por 165.00 con la referencia LEGADO original; B sigue VALIDADO | [Recuperacion](ui/15-A-recuperado-referencia-original.png), [SQLite/diario](ui/A-recuperado-sqlite.json) |
| Reprocesar Outbox dos veces desde la segunda ventana | Una venta, metrica 165.00 y un aviso; ningun duplicado | [Primer intento](ui/16-outbox-primer-reintento.png), [segundo](ui/17-outbox-segundo-reintento.png) |
| Volver a consultar inventario | P04 fisico 0, comprometido 0, disponible 0; un movimiento VENTA -4 | [Inventario final](ui/18-inventario-final-sin-compromiso-fantasma.png), [estado SQL final](ui/final-sqlite.json) |

Identidades observadas: A `e39fc6ce-363d-4179-8980-120deeaea580`; B `7cc07ac8-6c2d-4f01-87ba-c2c73e5bed14`; referencia unica `SIM-fd7087267acb4bb8ab755a61a072002e`, proveedor LEGADO. Clave inicial `R06-UI-A-ACEPTADO`, nueva clave `R06-UI-A-RECUPERAR`, intento B `R06-UI-B-RECHAZAR`.

Las cuatro inspecciones SQL son de solo lectura y se verificaron con [Verificar-Evidencia-R06.py](../../../scripts/Verificar-Evidencia-R06.py). Estado final: una aceptacion de 165.00, un pago, un movimiento de -4, un evento, un aviso y tres marcas de entrega a suscriptores. `integrity_check = ok`, sin errores FK. No se rehicieron las doce pantallas completas en esta revision; sus recorridos anteriores se conservan, y se verifico fisicamente el flujo afectado por R06.

## Conservacion, commits y paquete

La base normal `C:\Users\ASUS\AppData\Local\LogiMarket360\logimarket.db` conserva tamano y SHA-256: `844728f6eb67a110cb542a809855c23fb3b8d529eaf92bb3bd743ddfa80db6d8`. [Registro antes/despues](preservacion-base-usuario.json). Las pruebas y la interfaz usaron exclusivamente bases temporales; los procesos de la demostracion se cerraron al terminar.

| Commit | Avance comprobado |
| --- | --- |
| bc9b3bf | Reserva equivalente durable, validacion de disponibilidad, proteccion de ajustes, espera del diario y catorce regresiones |
| dbfe6dc | Existencias en WinForms y evidencia de 163 pruebas, P01-P38, dos procesos y reinicio real |

Los commits posteriores registran esta documentacion y la comprobacion de la copia limpia. El paquete `artifacts/APE04_LogiMarket360_R06_2026-11-22.zip` incluye HEAD, historial y SHA-256 de cada archivo en su manifiesto; conserva las revisiones originales y la evidencia anterior/despues, excluyendo Git interno, binarios generados, temporales y bases de ejecucion. La comprobacion de la copia limpia se incorpora al terminar ese empaquetado.

## Comandos de reproduccion

Desde la raiz del repositorio, con SDK y Desktop Runtime .NET 8 instalados:

```powershell
dotnet tool restore
dotnet restore .\LogiMarket360.sln --locked-mode
dotnet build .\LogiMarket360.sln --configuration Release --no-restore -warnaserror
dotnet test .\LogiMarket.Tests\LogiMarket.Tests.csproj --configuration Release --no-build --no-restore
$repoRevision = (Get-Location).Path
$anteriores = '.\docs\revisiones\2026-11-22\recibido\Revision_LogiMarket360_2026-11-22\pruebas\RevisionProbes.csproj'
$nueva = '.\docs\revisiones\segunda-2026-11-22\recibido\Segunda_Revision_LogiMarket360_2026-11-22\prueba_nueva\RevisionProbes.csproj'
dotnet restore $anteriores "-p:LogiMarketRepo=$repoRevision" --locked-mode
dotnet test $anteriores "-p:LogiMarketRepo=$repoRevision" --configuration Release --no-restore
dotnet restore $nueva "-p:LogiMarketRepo=$repoRevision" --locked-mode
dotnet test $nueva "-p:LogiMarketRepo=$repoRevision" --configuration Release --no-restore
python .\scripts\Resumir-R06.py
```

Para la demostracion manual, iniciar con `dotnet run --project .\LogiMarket.WinForms --configuration Release --no-build -- --db .\tmp\R06-manual\logimarket.db`. Usar una carpeta nueva: el README contiene las cuentas ficticias, seed y migraciones. Las pruebas automatizadas crean sus propias bases aisladas.

No hay comprobaciones funcionales pendientes de R06. Una base que ya fue sobrecomprometida por la version anterior o una aceptacion antigua sin PedidoId compatible requiere conciliacion manual: esta correccion no inventa stock ni reconstruye pagos historicos. La base de uso no fue intervenida ni usada para comprobar escenarios de conciliacion. GitHub y el informe academico formal siguen aplazados.
