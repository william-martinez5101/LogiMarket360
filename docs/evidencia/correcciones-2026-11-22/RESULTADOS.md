# Correcciones verificadas el 22 de noviembre de 2026

Repositorio: `C:\Users\ASUS\source\repos\LogiMarket360`, rama `codex/completar-logimarket360`. Inicio real: `b494016a280e1b32cd5b8d4b97f530a0bd550fb3`, arbol limpio y doce commits conservados. Se leyeron LEEME_Revision.md y Prompt_Correcciones_Codex.txt del adjunto, se verifico su manifiesto y se conservaron sus archivos en docs/revisiones/2026-11-22/recibido. La autorizacion de trabajo procede de la peticion del usuario; el adjunto aporta los hallazgos y casos de prueba adoptados por esa peticion.

## Resultado actual

- **149/149 pruebas**, sin fallidas ni omitidas: las **109 originales** conservadas y **40 regresiones**. Comparacion por nombres/cantidad de casos en [resumen-pruebas.json](resumen-pruebas.json).
- **5/5 pruebas adicionales adjuntas aprobadas**, ejecutadas como proyecto separado y con su fuente original intacta.
- **P01-P38 aprobadas otra vez**: [matriz con valores reales de esta ejecucion](P01-P38.md) y 38 JSON en aceptacion-final.
- Restore bloqueado y build Release con advertencias como errores: **0 errores y 0 advertencias**. Destinos .NET8 y Windows Forms, EF Core8/SQLite en Infrastructure; Domain/Application independientes de esas tecnologias.
- Interfaz Windows real verificada para las correcciones del carrito y del cobro; detalle y capturas abajo. La base normal conserva el SHA-256 inicial: [registro de preservacion](preservacion-base-usuario.json). No se aplicaron migraciones ni seed ni operaciones sobre ella.
- No quedan comprobaciones funcionales pendientes de los cinco puntos. La compatibilidad con un cobro antiguo sin identidad de pedido exige conciliacion manual y bloquea nuevos cargos; este rechazo seguro tambien tiene regresion. GitHub e informe academico formal siguen aplazados.

## Fallos antes y correcciones comprobadas

| Punto | Antes observado | Correccion y comprobacion | Commit |
| --- | --- | --- | --- |
| R01 Cobro | Caso adjunto: dos cargos por118 tras rollback y otra clave; suite original109 aprobada | Pedido como identidad durable; reserva de claves, recuperacion antes de invocar proveedor, alias con propietario. Ambos proveedores, reinicio/cambio de proveedor, concurrencia, cancelacion pendiente, COMMAND ERROR, stock y Outbox sin duplicados | a054cdb |
| R01 Compatibilidad | Un diario anterior sin PedidoId permitia otro cargo si no tenia pago local | Aceptacion antigua huerfana exige conciliacion; referencias ya conciliadas permiten nuevas ventas. Dos regresiones, incluida reproduccion fallida previa | 53583a7 |
| R02 Seed | P04 reaparecia al reiniciar; renombrar cliente podia recrearlo | Marca V1 en la transaccion inicial y adopcion de bases anteriores sin reconstruccion. Bajas P04/kit, Ana renombrada/inactiva, precio/stock/pagos conservados; interrupcion real por trigger SQLite y reintento completo | ed26994 |
| R03 Carrito | Windows anterior: P04 luego P01, orden ascendente, seleccion P01; Quitar borraba P04 | Fila enlazada con ordinal del modelo por render, incluyendo duplicados. Windows asc/desc, cambiar/quitar, undo/redo, guardar/recargar tras reinicio y totales; regresiones con SQLite | c6d9f5f |
| R04 Instantaneas | Application aceptaba precio P01 inventado; kit/composicion manipulables | Captura de nuevas lineas desde catalogo; coincidencia completa y consumible de historicas persistidas del propio borrador. Precio/raiz/composicion alterados, duplicados nuevos, codigo ajeno, pago59 y devolucion54 historicos | e207608 |
| R05 Filtros | `CLIENTE = =` y `CLIENTE = >=` se aceptaban | Tokens tipados, valores validos y error con posicion real. Operadores/conectores/numeros como nombre sin comillas rechazados; nombres Unicode/comillas, AST, precedencia y parentesis conservados | 14639f2 |

No se editaron ni eliminaron los metodos de las109 pruebas originales. Solo se amplio el cableado de su fixture Escenario para el nuevo puerto de intentos; las regresiones son archivos nuevos. Las restricciones, la migracion original y los requisitos PDF se conservan. El seed utiliza una marca con PK fija en la tabla existente Auditorias; no necesita reescribir la migracion ni reiniciar bases.

## Registros de ejecucion

| Verificacion | Resultado real | Registro |
| --- | --- | --- |
| Originales antes |109 aprobadas | [TRX](antes/originales-109.trx) |
| Adjuntas antes |5 fallidas, reproducidas | [TRX](antes/adicionales-fallos.trx) |
| R01 antes/despues |7 fallidas ->54 aprobadas incluyendo aceptaciones; adjunta REV01 aprobada | antes/R01-antes.trx; despues/R01-despues.trx; despues/R01-adjunta.trx |
| R01 legado antes/despues |1 fallida/1 aprobada ->9 regresiones de cobro aprobadas | antes/R01-legado-antes.trx; despues/R01-legado-despues.trx |
| R02 antes/despues |3 fallidas ->5 aprobadas incluyendo P01/P10 | antes/R02-antes.trx; despues/R02-despues.trx |
| R03 despues |2 aprobadas; reproduccion previa interactiva conservada | despues/R03-despues.trx; ui/R03-antes-*.png |
| R04 antes/despues |7 fallidas/1 aprobada ->13 aprobadas incluyendo aceptaciones afectadas | antes/R04-antes.trx; despues/R04-despues.trx |
| R05 antes/despues |10 fallidas/8 aprobadas ->20 aprobadas incluyendo P25/P26 | antes/R05-antes.trx; despues/R05-despues.trx |
| Restore bloqueado |Correcto | [solucion](restore-final.log), [adjuntas](restore-adjuntas-final.log) |
| Build Release -warnaserror |0 errores,0 advertencias | [build-final.log](build-final.log) |
| Suite final |149 aprobadas,0 omitidas | [suite-final.trx](final/suite-final.trx) |
| Adjuntas finales |5 aprobadas,0 omitidas | [adjuntas-final.trx](final/adjuntas-final.trx) |

Las pruebas de integracion crean directorios temporales distintos, con migraciones SQLite reales; ninguna usa la base normal. Los JSON de R01-R05 y aceptacion-final se generan despues de las aserciones sobre la base y los diarios reales, con fechas UTC.

## Windows real

Se usaron binarios publicados en tmp/ui-revision-antes y tmp/ui-revision-despues, con bases separadas y `--db` explicito. La verificacion final tras otro reinicio uso tmp/ui-revision-final, compilado con el codigo de53583a7. Las capturas son de la aplicacion ejecutada mediante la habilidad computer-use; cada PNG conservado incluye un JSON de accesibilidad. No se usaron wireframes como evidencia.

| Paso observado | Resultado real | Evidencia |
| --- | --- | --- |
| Reproduccion anterior | P01 seleccionado tras ordenar; desaparecio P04 y quedo P01, total23 | [seleccion anterior](ui/R03-antes-seleccion-P01.png), [error anterior](ui/R03-antes-quito-P04-incorrecto.png) |
| Quitar en ascendente | Se quita P01; queda P04, total41 | [resultado corregido](ui/R03-despues-quitar-P01-correcto.png) |
| Deshacer/Rehacer | Recupera P04/P01 total59 y vuelve a quitar P01 total41 | [deshacer](ui/R03-deshacer.png), [rehacer](ui/R03-rehacer.png) |
| Duplicados | P04/P01/P01, seleccion de linea3 tras ordenar; cambiar a2 deja linea2 en1, total95 | [seleccion](ui/R03-duplicado-linea3-seleccionado.png), [cantidad correcta](ui/R03-duplicado-cantidad-correcta.png) |
| Descendente | P04/linea3-P01/linea2-P01; seleccionar linea2 y quitar conserva P04x1/P01x2, total77 | [seleccion descendente](ui/R03-desc-seleccion-linea2.png), [resultado](ui/R03-desc-quita-linea2-correcto.png) |
| Guardado y reinicio | Mismo pedido7c57d0f9, P04x1/P01x2, total77 al cargar despues de reiniciar | [guardado](ui/R03-guardado-77.png), [recarga](ui/R03-recarga-tras-reinicio-77.png) |
| Pedido de cobro | Ana VIP, teletrabajo1 estandar,60-6+5=59; VALIDADO | [carrito](ui/R01-carrito-59.png), [validacion](ui/R01-pedido-validado.png) |
| Fallo posterior a aceptacion | Fallar guardado aplicado; UI-INTENTO-A aceptado por LEGADO; error en UI, sigue VALIDADO | [fallo aplicado](ui/R01-escenario-fallo-aplicado.png), [error](ui/R01-fallo-despues-aceptacion.png) |
| Inspeccion tras fallo |1 cargo59;0 pagos/movimientos/eventos/avisos; stock8/5/20/4/30; COMMAND ERROR; integridad ok | [SQLite/diario tras fallo](ui/R01-fallo-sqlite.json) |
| Reinicio y reintento | Otra clave UI-REINTENTO-B, proveedor seleccionado ALTERNATIVO; recupera LEGADO y SIM-d4fb0f2a61e141709997e71b93f5ecd4; PAGADO59 | [entrada distinta](ui/R01-reintento-otra-clave-proveedor.png), [referencia recuperada](ui/R01-recuperado-misma-referencia.png) |
| Inspeccion tras recuperar |1 cargo/pago,3 movimientos,1 evento procesado/aviso/metrica y3 marcas de suscriptor; stock7/4/19/4/30 | [SQLite/diario recuperado](ui/R01-recuperado-sqlite.json) |
| Otro reinicio, codigo final y clave C | UI-REINTENTO-C sobre PAGADO devuelve la misma referencia original; no suma otro cargo/pago/aviso | [captura final](ui/R01-tercera-clave-mismo-pago-final.png), [SQLite/diario final](ui/R01-reabierto-sqlite.json) |

El pedido de cobro es8ec4c452-64b8-4371-96ee-5c5e99c81ac4. El script de inspeccion usa SQLite en modo de solo lectura y comprueba los valores; solo admite bases dentro de tmp. Todas las instancias de prueba se cerraron al terminar. El recorrido anterior de despacho, entrega, devolucion y las doce vistas sigue conservado en RECORRIDO_WINDOWS.md; no se afirma haber repetido esas otras pantallas en esta revision. Sus aceptaciones automaticas se ejecutaron nuevamente.

## Comandos ejecutados y reproduccion

Desde la raiz del repositorio, en PowerShell:

```powershell
dotnet restore .\LogiMarket360.sln --locked-mode
dotnet build .\LogiMarket360.sln --configuration Release --no-restore -warnaserror
$env:LOGIMARKET_EVIDENCE = Join-Path (Get-Location) 'docs\evidencia\correcciones-2026-11-22\aceptacion-final'
dotnet test .\LogiMarket.Tests\LogiMarket.Tests.csproj --configuration Release --no-build --no-restore --logger 'trx;LogFileName=suite-final.trx' --results-directory .\docs\evidencia\correcciones-2026-11-22\final
$repoRevision = (Get-Location).Path
$proyectoRevision = '.\docs\revisiones\2026-11-22\recibido\Revision_LogiMarket360_2026-11-22\pruebas\RevisionProbes.csproj'
dotnet restore $proyectoRevision "-p:LogiMarketRepo=$repoRevision" --locked-mode
dotnet test $proyectoRevision "-p:LogiMarketRepo=$repoRevision" --configuration Release --no-restore --logger 'trx;LogFileName=adjuntas-final.trx' --results-directory .\docs\evidencia\correcciones-2026-11-22\final
python .\scripts\Resumir-Correcciones.py
# Despues de ejecutar desde la UI cada fase sobre su base temporal:
python .\scripts\Verificar-Evidencia-Windows.py .\tmp\ui-revision-despues\datos\logimarket.db fallo .\docs\evidencia\correcciones-2026-11-22\ui
python .\scripts\Verificar-Evidencia-Windows.py .\tmp\ui-revision-despues\datos\logimarket.db recuperado .\docs\evidencia\correcciones-2026-11-22\ui
python .\scripts\Verificar-Evidencia-Windows.py .\tmp\ui-revision-despues\datos\logimarket.db reabierto .\docs\evidencia\correcciones-2026-11-22\ui
```

Las fases Windows deben inspeccionarse en ese orden mientras se ejecuta el recorrido: el script lee el estado real, no reconstruye el estado anterior. Python3 se usa solo para validar evidencia y paquetes; no es dependencia del programa. La aplicacion y las pruebas C# usan las dependencias oficiales fijadas en README.

## Paquete para revision

Se genera `artifacts/APE04_LogiMarket360_Correcciones_2026-11-22.zip` despues de registrar los cambios. MANIFIESTO_REVISION.json identifica HEAD, cada archivo y su SHA-256; HISTORIAL_COMMITS.txt conserva la secuencia completa. El repositorio conserva su .git y el ZIP excluye bases, diarios de uso, binarios generados y archivos de entorno. La comprobacion de la copia limpia se registra al concluir el empaquetado.
