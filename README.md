# LogiMarket 360

Aplicacion de escritorio C# **.NET 8 / Windows Forms**, con **SQLite y Entity Framework Core 8.0.34**. Incluye ventas, inventario, pagos simulados, despacho, entrega, devoluciones, auditoria, Outbox y los 23 patrones GoF integrados. No calcula IVA. Los comprobantes son internos y todas las integraciones son locales.

Actualizacion R06: los cobros aceptados pendientes de commit comprometen sus unidades de forma durable. Las otras ventas y los ajustes negativos solo pueden consumir unidades libres; el propietario recupera su cobro y consume sus propias unidades una sola vez. [Diseno de la reserva equivalente](docs/RESERVAS_EXISTENCIAS.md) y [resultados/evidencia R06](docs/evidencia/R06-2026-11-22/RESULTADOS.md).

## Requisitos e instalacion

Windows 10/11, SDK .NET 8 y Windows Desktop Runtime 8; Git para preparar nuevamente el ZIP; Visual Studio con desarrollo de escritorio .NET si se utiliza el IDE. Un SDK posterior tambien compila los destinos net8.0/net8.0-windows. La solucion `.sln` permite usar el SDK 8; se conserva la `.slnx` original para herramientas compatibles.

Instalacion oficial del SDK en Windows, si hace falta:

```powershell
winget install --id Microsoft.DotNet.SDK.8 --exact --source winget
dotnet --list-sdks
dotnet --list-runtimes
```

Desde la carpeta de la solucion:

```powershell
dotnet tool restore
dotnet restore .\LogiMarket360.sln --locked-mode
dotnet build .\LogiMarket360.sln --configuration Release --no-restore -warnaserror
```

El manifiesto fija dotnet-ef 8.0.34. Infrastructure fija Microsoft.EntityFrameworkCore.Sqlite y Design 8.0.34; WinForms fija Microsoft.Extensions.DependencyInjection 8.0.1. Los archivos packages.lock.json conservan las dependencias transitivas. Domain y Application no tienen dependencias de EF, SQLite ni WinForms.

## Migraciones, seed y ejecucion

La ruta normal es `%LOCALAPPDATA%\LogiMarket360\logimarket.db`. Al arrancar se aplican las migraciones pendientes y se recupera Outbox. Los datos oficiales se siembran **una sola vez**, con una marca durable `DATOS_DEMO_V1` en Auditorias y dentro de la misma transaccion. Los siguientes arranques conservan bajas, nombres, precios, kits, stock y operaciones. Una base de la version anterior que ya contiene datos se adopta sin reponer faltantes. Una inicializacion interrumpida se revierte completa y puede reintentarse.

Comandos explicitos equivalentes al inicio automatico:

```powershell
$env:LOGIMARKET_DB = Join-Path $env:LOCALAPPDATA 'LogiMarket360\logimarket.db'
dotnet ef database update --project .\LogiMarket.Infrastructure --configuration Release --no-build
dotnet run --project .\LogiMarket.WinForms --configuration Release --no-build -- --initialize
dotnet run --project .\LogiMarket.WinForms --configuration Release --no-build
```

`--initialize`, `--seed` y `--migrate` aplican migraciones y seed y terminan dejando `inicializacion.log` junto a la base. `--db RUTA` tiene prioridad sobre LOGIMARKET_DB para la aplicacion. El comando de EF usa LOGIMARKET_DB. No se utiliza EnsureCreated.

Para crear una nueva migracion de desarrollo:

```powershell
dotnet ef migrations add NombreDescriptivo --project .\LogiMarket.Infrastructure --output-dir Persistencia\Migraciones
```

Para una demostracion limpia use **un directorio nuevo**, sin borrar la base normal:

```powershell
dotnet run --project .\LogiMarket.WinForms --configuration Release --no-build -- --db .\tmp\demo-nueva\logimarket.db
```

Junto a la base se conservan `cobros-simulados.json` (aceptaciones durables del proveedor, ligadas al pedido), `intentos-cobro.json` (identidad del intento y claves reservadas), `operaciones.jsonl` (traza tecnica) y `comprobantes\`. Una base de demostracion independiente debe tener su propio directorio. Conserve ese directorio completo para recuperar el mismo entorno.

## Cuentas ficticias

Todas utilizan la clave de demostracion `Demo360!`. SQLite almacena PBKDF2-SHA256 con sal aleatoria, iteraciones y derivado; no almacena esa clave en texto claro.

| Usuario | Rol | Operaciones |
| --- | --- | --- |
| admin.demo | ADMIN | Todas, incluido catalogo y escenarios de fallo |
| ventas.demo | VENTAS | Clientes, borradores, validacion, cobro y devolucion |
| bodega.demo | BODEGA | Inventario/ajustes, despacho y entrega |
| consulta.demo | CONSULTA | Consultas y comprobantes |

Todos los roles autenticados pueden consultar. Las devoluciones se asignan a VENTAS/ADMIN; los ajustes a BODEGA/ADMIN; el mantenimiento de catalogo y la configuracion de fallos a ADMIN. Application y PagoAutorizadoProxy aplican estos permisos independientemente de los botones.

## Recorrido de demostracion

1. Ingrese como admin.demo. Revise Catalogo y kits y Clientes. Ana es VIP.
2. En Carrito seleccione Ana, KIT_TELETRABAJO, cantidad 1 y ESTANDAR. Agregue la linea: subtotal 60.00, descuento 6.00, tarifa 5.00, total 59.00. Deshacer/Rehacer conservan el borrador completo, hasta cinco estados.
3. Guarde y valide. En Cobro confirme con la clave propuesta. El pedido queda PAGADO y P01/P02/P03 quedan en 7/4/19. Repetir el pedido con la misma clave u otra clave libre devuelve el pago original. Una clave reservada a otro pedido se rechaza antes de cobrar.
4. Cierre la aplicacion y vuelva a ejecutarla con la misma ruta. El pedido y las existencias permanecen guardados.
5. Con BODEGA registre despacho y entrega. Con ADMIN o VENTAS procese DEFECTO: reembolso 54.00, sin flete. Una segunda devolucion se rechaza.
6. Reportes ofrece venta/devolucion por Vista previa y TXT. Pedidos admite `ESTADO = PAGADO AND TOTAL >= 50`, `CLIENTE = 'Ana'`, AND, OR y parentesis; AND tiene prioridad.
7. Auditoria/pruebas permite seleccionar proveedor/canal, inducir rechazo de cobro, fallo de aviso o fallo del guardado del cobro, reprocesar Outbox y demostrar los recorridos de kits y Flyweight. El conjunto Flyweight conserva 1000 filas y se presenta en paginas de 50 para facilitar el acceso a los controles.

Para demostrar recuperacion, active Fallar notificador, confirme un pedido y observe PENDIENTE. Cierre y vuelva a abrir: los fallos de demostracion se reinician y el aviso pendiente se recupera. Los suscriptores ya aplicados no se repiten.

Para demostrar la recuperacion del cobro, active **Fallar guardado del cobro** y pulse Aplicar escenario. Confirme un pedido VALIDADO con clave A: el proveedor acepta, el commit falla y la UI conserva VALIDADO con un error legible. Cierre/reabra sobre el mismo directorio (o desactive el fallo y aplique), cambie a una clave B y, si desea, a otro proveedor. Confirmar recupera la referencia/proveedor originales y confirma pago, stock y Outbox una sola vez. No cancele un pedido con una aceptacion pendiente: primero concilie confirmandolo.

Application captura del catalogo los precios y la composicion de las lineas nuevas. Al editar un borrador, solo conserva instantaneas historicas que coincidan completamente con lineas previamente persistidas de ese pedido, una vez por ocurrencia; cambiar cantidad conserva su instantanea, agregar otra linea captura el catalogo actual. Los pedidos pagados y sus devoluciones mantienen sus valores historicos. En el carrito, ordenar la tabla no cambia la identidad de la linea seleccionada, incluso con SKU repetidos.

Para repetir R06 con datos nuevos: cree y valide A y B de Luis, cada uno P04 x4 y ESTANDAR, total 165.00. Active el fallo del guardado y confirme A. Inventario muestra P04 fisico 4 / comprometido 4 / disponible 0. Desactive el fallo o use otra instancia sobre la misma base: confirmar B se rechaza antes de cobrar. Cierre/reabra y confirme A con otra clave: recupera su referencia, queda PAGADO y las existencias son 0/0/0. Solo se comprometen las cantidades de A; los SKU y cantidades libres siguen vendiendose. Una aceptacion que aun no tiene pago local se protege usando el diario y las lineas inmutables persistidas del pedido VALIDADO. Al confirmar el pago, ese mismo commit consume el compromiso; no hay una liberacion posterior que pueda perderse.

## Pruebas y evidencia

```powershell
$env:LOGIMARKET_EVIDENCE = Join-Path (Get-Location) 'docs\evidencia\aceptacion'
dotnet test .\LogiMarket.Tests\LogiMarket.Tests.csproj --configuration Release --no-restore --logger 'trx;LogFileName=verificacion.trx' --results-directory .\docs\evidencia\reproduccion
```

Pruebas de la revision independiente (proyecto separado, casos recibidos sin cambios):

```powershell
$repoRevision = (Get-Location).Path
$proyectoRevision = '.\docs\revisiones\2026-11-22\recibido\Revision_LogiMarket360_2026-11-22\pruebas\RevisionProbes.csproj'
dotnet restore $proyectoRevision "-p:LogiMarketRepo=$repoRevision" --locked-mode
dotnet test $proyectoRevision "-p:LogiMarketRepo=$repoRevision" --configuration Release --no-restore --logger 'trx;LogFileName=adjuntas.trx' --results-directory .\docs\evidencia\reproduccion
```

Nueva prueba externa de la segunda revision (tambien incorporada al proyecto principal, sin cambiar su cuerpo/aserciones):

```powershell
$proyectoR06 = '.\docs\revisiones\segunda-2026-11-22\recibido\Segunda_Revision_LogiMarket360_2026-11-22\prueba_nueva\RevisionProbes.csproj'
dotnet restore $proyectoR06 "-p:LogiMarketRepo=$repoRevision" --locked-mode
dotnet test $proyectoR06 "-p:LogiMarketRepo=$repoRevision" --configuration Release --no-restore --logger 'trx;LogFileName=R06.trx' --results-directory .\docs\evidencia\reproduccion
```

Cada escenario crea un archivo SQLite nuevo en `%TEMP%\LogiMarket360Tests\<guid>\prueba.db`, con un directorio independiente y migraciones reales. No utiliza EF InMemory ni la base normal. P36 abre servicios y conexiones independientes y coordina dos confirmaciones concurrentes. Las pruebas de rechazo verifican ausencia de efectos indebidos.

- [Matriz P01-P38 y resultados reales](docs/evidencia/ACEPTACION.md).
- [Recorrido real en Windows y capturas](docs/evidencia/RECORRIDO_WINDOWS.md).
- [Arquitectura, requisitos y decisiones](docs/ARQUITECTURA.md).
- [Matriz de los 23 patrones](docs/PATRONES.md).
- [Riesgos, reparaciones y limites](docs/DECISIONES.md).
- [Estado y commits de entrega](docs/ESTADO.md).

Resultado actual R06: **163/163 pruebas** (149 anteriores conservadas y 14 casos nuevos), **5/5 externas anteriores** y **1/1 externa R06**; P01-P38 aprobadas nuevamente. Restore bloqueado y build Release con advertencias como errores: 0 errores, 0 advertencias. El [registro R06](docs/evidencia/R06-2026-11-22/RESULTADOS.md) conserva el fallo previo por 330.00, las regresiones y el recorrido real con dos procesos Windows, reinicio, A recuperado por 165.00 y B sin cobro. La base de uso del usuario conserva su huella inicial. Las evidencias anteriores de 109 y 149 pruebas, incluidas las correcciones R01-R05, se conservan como registros historicos.

La entrega anterior de R01-R05 tambien se verifico desde una copia limpia con SDK8.0.420,149/149 y5/5, migracion y seed doble. Su [registro historico](docs/evidencia/correcciones-2026-11-22/copia-limpia/verificacion.json) corresponde a ese codigo anterior; la verificacion del paquete R06 se registra junto a su evidencia actual.

Los JSON por aceptacion contienen esperado, obtenido, fecha, metodo, stock, pagos, eventos y trazas. Los TRX conservan tanto la ejecucion inicial fallida como las reparaciones y la verificacion final. Las capturas proceden de la aplicacion ejecutada; los wireframes del PDF son referencias.

## Garantias y alcance tecnico

Pedido/pago/stock/movimientos/Outbox se confirman en una transaccion SQLite de escritura adquirida antes de consultar disponibilidad o llamar al proveedor. Version de pedido y token de stock protegen escrituras obsoletas; restricciones unicas protegen pago por pedido, referencia, clave de idempotencia, movimientos, despacho y devolucion. `IIntentosCobro`, implementado en Infrastructure, reserva durablemente las claves y asocia la aceptacion al pedido fuera del rollback comercial. Recupera ese resultado antes de invocar otra vez la pasarela, aun cambiando clave/proveedor o reiniciando. Un diario antiguo con una aceptacion sin pedido y sin pago local correspondiente bloquea nuevos cargos y exige conciliacion manual; los pagos antiguos ya conciliados siguen admitiendo nuevas ventas. No hay transacciones distribuidas ni cobros bancarios.

Cada entrega Outbox se confirma junto con el efecto del suscriptor y su clave evento/suscriptor. Las notificaciones son registros locales durables. MetricasSesion es unico en DI y sincroniza ids durables sin volver a sumarlos; la base es la fuente de verdad. Las fechas de operacion se almacenan en UTC y se muestran en la zona local de Windows; los plazos de devolucion comparan fechas UTC, incluyendo el dia limite.

La disponibilidad se comprueba bajo ese mismo BEGIN IMMEDIATE descontando, por SKU agregado, las aceptaciones pendientes de otros pedidos. Se consulta el stock fisico real y las instantaneas persistidas; no se omite la validacion al reintentar. Un rechazo del proveedor no compromete unidades; un pago confirmado deja de contarse como compromiso porque ya desconto el stock en su transaccion. Inventario y Catalogo distinguen Fisico, Comprometido y Disponible. La base normal no se modifica para las pruebas y esta correccion no necesita una migracion adicional.

## ZIP para revision

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Preparar-Revision.ps1
# Nombre especifico del paquete actual R06:
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Preparar-Revision.ps1 -Destino .\artifacts\APE04_LogiMarket360_R06_2026-11-22.zip
```

El comando con Destino genera `artifacts\APE04_LogiMarket360_R06_2026-11-22.zip`; sin ese parametro usa el nombre general `APE04_LogiMarket360_Revision.zip`. Solo incluye archivos versionados, con HEAD, historial, manifiesto y huella SHA-256. Excluye .git, .vs, bin, obj, tmp, bases de ejecucion, configuraciones locales y secretos. El historial Git completo se conserva en el repositorio. GitHub y el informe academico formal permanecen fuera de esta entrega por solicitud del usuario.
