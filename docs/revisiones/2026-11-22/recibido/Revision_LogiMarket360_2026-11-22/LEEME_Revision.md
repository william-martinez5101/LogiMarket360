# Revisión independiente de LogiMarket360

Fecha: 22 de noviembre de 2026. Revisión de la carpeta de trabajo que el usuario copió desde `C:\Users\ASUS\source\repos\LogiMarket360` y comprimió como `LogiMarket360.zip`.

## Resultado

La solución completa compila y sus 109 pruebas originales pasan. El historial contiene 12 commits y las implementaciones de los 23 patrones solicitados están presentes. Sin embargo, esta revisión encontró cuatro problemas reproducidos mediante cinco casos adicionales, más un problema de selección de líneas del carrito detectado mediante inspección del código.

Conviene corregirlos antes de considerar cerrada la revisión funcional. El caso de mayor prioridad permite que la pasarela de demostración registre dos cobros para un mismo pedido después de un fallo de persistencia. Que las 109 pruebas originales pasen no cubre este escenario.

Este paquete documenta la revisión; el código productivo de la copia recibida se conservó. Las pruebas adicionales se crearon en un proyecto separado. Los archivos `bin` y `obj` producidos al compilar son resultados de la verificación.

## Comprobaciones realizadas

| Comprobación | Resultado | Evidencia |
| --- | --- | --- |
| Restauración de los cinco proyectos con dependencias bloqueadas | Correcta | `evidencia/restore-solucion.log` |
| Compilación Release de Domain, Application, Infrastructure, WinForms y Tests | 0 errores y 0 advertencias; advertencias tratadas como errores | `evidencia/build-solucion.log` |
| Suite original | 109 ejecutadas, 109 aprobadas, 0 fallidas, 0 omitidas | `evidencia/originales.trx` |
| Casos adicionales de esta revisión | 5 ejecutados, 5 fallidos, correspondientes a cuatro problemas | `evidencia/adicionales.trx` y `pruebas/RevisionAdicionalTests.cs` |
| Archivos seguidos por Git frente a HEAD | 238 archivos presentes, sin diferencias de contenido al normalizar CRLF/LF | Comparación con los objetos del repositorio recibido |
| Historial | 12 commits; los cuatro iniciales conservados y ocho posteriores | Rama y objetos Git de la copia recibida |
| ZIP limpio incluido en artifacts | 240 entradas; 238 archivos del manifiesto con hashes correctos; fuentes coincidentes con esta copia al normalizar finales de línea | `artifacts/APE04_LogiMarket360_Revision.zip` del proyecto recibido |
| Evidencia de interfaz incluida | Se inspeccionaron registros y capturas, incluida la del carrito de Ana por USD 59.00 | `docs/evidencia` de la copia recibida |

Entorno de esta verificación: Linux, SDK .NET 8.0.420, runtime .NET 8.0.26. La compilación de WinForms se realizó con `EnableWindowsTargeting=true`. Esto comprueba la compilación del proyecto Windows desde Linux; la ejecución interactiva de la interfaz Windows queda por repetir en el equipo del usuario. Las capturas incluidas en el proyecto corresponden a su verificación anterior.

Se utilizó MSBuild con un solo proceso para ajustarse a este entorno. Un intento de restauración en paralelo no había completado correctamente aquí; no se considera por ello un defecto del proyecto.

## Hallazgos y correcciones

### R01. Dos cobros del proveedor después de rollback y cambio de clave

Prioridad: alta. Reproducido ejecutando código real de Application e Infrastructure contra SQLite temporal.

Ubicaciones: `LogiMarket.Application/Pedidos/ConfirmacionPedidoFacade.cs`, líneas 15-27 y 37; `LogiMarket.Infrastructure/Pagos/DiarioCobrosSimulados.cs`, método `Aprobar`. La clave es editable en `LogiMarket.WinForms/Vistas/OperacionPedidoVistas.cs`.

Secuencia reproducida:

1. Crear y validar el pedido oficial de Ana por USD 59.00.
2. Activar el fallo de persistencia ofrecido por el proyecto.
3. Confirmar con la clave `revision-intento-1`. La pasarela acepta el cobro, pero SQLite revierte el guardado.
4. Desactivar el fallo y confirmar el mismo pedido con `revision-intento-2`.

Resultado observado: un pago local, dos cobros aceptados por la pasarela de demostración y USD 118.00 acumulados. La prueba `REV01_ReintentoConOtraClaveTrasRollback_NoDebeDuplicarCobro` falla porque encuentra dos cobros.

La unicidad de `Pagos` y la deduplicación por clave protegen los reintentos con la misma clave, pero no relacionan durablemente el intento ya aceptado con el pedido cuando el commit local falló. La restricción local por pedido no puede deshacer un segundo cobro externo. Este caso contradice la regla general del PDF que impide cobrar un pedido más de una vez.

Corrección: asegurar una identidad de cobro estable por pedido y una recuperación durable del intento aceptado. Antes de autorizar otro cobro, un reintento con otra clave debe recuperar/reconciliar el intento original o rechazar el cambio sin volver a cobrar. Debe conservarse esta protección tras reiniciar y al cambiar entre las pasarelas disponibles. Bloquear solamente el campo de la interfaz no protege las llamadas al caso de uso.

Aceptación: tras fallo de guardado, reintentos con la misma clave y con otra clave, incluso tras reiniciar, dejan un solo cobro aceptado, un pago local final y un movimiento de venta por SKU. También debe rechazarse la reutilización de una clave para otro pedido.

### R02. El seed restaura un producto que se eliminó correctamente

Prioridad: media. Reproducido con SQLite temporal y el inicializador real del arranque.

Ubicación: `LogiMarket.Infrastructure/Persistencia/InicializadorBase.cs`, líneas 18-23 y 32-35.

Se eliminó P04 mediante `CatalogoServicio.EliminarProductoAsync`. P04 no tenía referencias que impidieran su borrado. La consulta posterior devolvió cuatro productos y confirmó su ausencia. Al crear otra instancia sobre la misma base y ejecutar `MigrarYSeedAsync`, el listado volvió a tener cinco productos: P04 reapareció con sus valores iniciales.

Prueba: `REV02_ProductoEliminado_DebeSeguirEliminadoTrasReiniciar`.

La comprobación "insertar cada SKU oficial si falta" evita duplicados en sucesivas ejecuciones, pero no distingue una base sin inicializar de un borrado autorizado. La misma forma de inicialización por nombre de clientes merece revisión: cambiar el nombre de un cliente inicial puede provocar que el seed inserte otro con el nombre original.

Corrección: registrar de forma transaccional que los datos iniciales ya se aplicaron, o usar una estrategia equivalente que preserve altas, ediciones y bajas posteriores. La base nueva debe seguir recibiendo los cinco productos, dos kits, clientes y roles oficiales; reiniciar una base en uso debe conservar sus cambios. Si se necesita restaurar la demostración, debe ser una acción explícita.

Aceptación: ejecutar seed dos veces en una base nueva no duplica datos; eliminar P04 y reiniciar conserva el borrado; editar un cliente inicial y reiniciar conserva la edición sin crear un cliente sustituto. La prueba adicional actual cubre el primer escenario de borrado; la edición de clientes se señala por inspección y requiere su propia prueba.

### R03. Ordenar el carrito puede hacer que se modifique otra línea

Prioridad: alta por el efecto sobre la venta. Detectado por inspección del código; pendiente de reproducción interactiva en Windows.

Ubicaciones: `LogiMarket.WinForms/Vistas/CarritoVista.cs`, líneas 26-27, 42 y 69; `LogiMarket.WinForms/Vistas/Ui.cs`, líneas 34-36 y 63-71.

`Indice()` devuelve `CurrentRow.Index`, la posición visible de la fila. `CambiarCantidad` y `Quitar` usan esa posición para acceder a la lista original de líneas. La tabla recibe una lista aparte de proyecciones y permite ordenarla mediante `ListaOrdenable<T>`; ordenar esa lista no ordena `_borrador.Lineas`.

Reproducción dirigida en Windows:

1. En un borrador nuevo, agregar primero P04 y después P01.
2. Ordenar la columna Articulo de forma ascendente: P01 pasa a ser la primera fila visible.
3. Seleccionar P01 y pulsar Quitar línea.
4. Comprobar que se quita P01. El código actual usa el índice cero y apunta a P04, que se agregó primero.
5. Repetir con Cambiar cantidad y comprobar Deshacer/Rehacer.

Corrección: identificar la línea desde el objeto enlazado o una identidad estable; no usar la posición visual como posición del modelo. El campo `Linea` de la proyección ya conserva el ordinal original durante ese renderizado, aunque una identidad de línea explícita permite manejar mejor duplicados del mismo SKU.

Aceptación: cambios y borrados afectan siempre a la línea seleccionada, con orden ascendente/descendente, productos y kits repetidos, y mantienen la consistencia de totales e historial. Este paquete no presenta una prueba Windows ejecutada para este hallazgo.

### R04. RegistrarAsync acepta una instantánea de precio fabricada por el llamador

Prioridad: media; validación de la frontera de Application. Reproducido mediante llamada directa al caso de uso, no mediante un control visible de edición de precios de WinForms.

Ubicación: `LogiMarket.Application/Pedidos/PedidosServicio.cs`, líneas 10-17; construcción pública de `NodoInstantanea` en `LogiMarket.Domain/Ventas/Instantaneas.cs`.

Con Luis NORMAL, el catálogo contiene P01 a USD 20.00. Se construyó un `PedidoBorrador` con el mismo SKU y un `NodoInstantanea` a USD 1.00. El caso de uso lo registró, validó y cobró por USD 6.00, incluidos USD 5.00 de envío. El importe esperado a partir del catálogo era USD 25.00, o un rechazo previo al cobro.

Prueba: `REV03_RegistrarPedido_NoDebeAceptarPrecioInventadoPorElLlamador`.

La interfaz actual captura normalmente los precios desde el catálogo, por lo que esta prueba no demuestra que el usuario pueda introducir USD 1.00 desde el formulario existente. Demuestra que Application confía en una instantánea creada fuera de su control y no garantiza por sí misma su procedencia.

Corrección: capturar o validar en Application los datos de catálogo al registrar nuevas líneas, incluyendo la composición del kit y sus cantidades. Definir cómo se distinguen las instantáneas históricas legítimas de las entradas nuevas. Conservar los precios y las composiciones históricos de pedidos ya registrados según las reglas del caso; recalcular un pedido pagado desde el catálogo actual romperia esa garantía.

Aceptación: un nuevo pedido no puede pagar un precio/composición inventados por un llamador; el cambio posterior del catálogo sigue sin alterar los pedidos pagados y las devoluciones siguen usando el importe histórico.

### R05. El Interpreter acepta operadores donde requiere un nombre de cliente

Prioridad: baja. Dos casos reproducidos.

Ubicación: `LogiMarket.Application/Consultas/FiltroPedidos.cs`, líneas 54-59.

Las expresiones `CLIENTE = =` y `CLIENTE = >=` se aceptan como filtros de nombre en vez de producir un error de sintaxis. `Siguiente()` entrega un token y solo se descartan nombres vacíos, paréntesis, AND y OR. Los tokens de operadores no se validan como tales.

Pruebas: las dos entradas de `REV04_Interpreter_RechazaUnOperadorComoValorDeCliente`.

Corrección: validar el tipo de token permitido para el nombre, conservar el soporte de cadenas entre comillas y emitir un error legible con posición cuando falte un valor. La regla P26 requiere rechazar sintaxis inválida. Esto es un problema de validación del lenguaje; en el camino revisado se construye un AST y no se concatena SQL.

Aceptación: ambos filtros anteriores se rechazan; `CLIENTE = 'Ana'`, filtros válidos de estado/total y la precedencia de AND/OR/paréntesis siguen funcionando.

## Estructura, patrones y evidencia

Los cinco proyectos mantienen la dirección principal de dependencias esperada: Domain no depende de EF; Application depende de Domain y de sus puertos; Infrastructure implementa persistencia e integraciones; WinForms compone servicios e interfaz. Tests referencia las capas que comprueba. Se encontraron migraciones reales, restricciones SQLite, unidades de trabajo, control de concurrencia, estados encapsulados, instantáneas de venta y entregas Outbox por suscriptor.

La tabla identifica las colaboraciones revisadas. Su presencia y las pruebas originales aprobadas no sustituyen las correcciones de los escenarios adicionales.

| Patrón | Colaboración localizada |
| --- | --- |
| Factory Method | `CreadorNotificacion`, `CreadorEmail`, `CreadorSms`; selección de notificador por canal |
| Abstract Factory | `FabricaEntrega`, `EntregaEstandarFactory`, `EntregaExpresFactory`; tarifa, plazo y etiqueta de una familia |
| Builder | `PedidoBuilder`; construcción y valoración de pedidos |
| Prototype | Clonación de borradores e instantáneas sin compartir datos mutables |
| Singleton | `MetricasSesion` registrada como servicio único en la composición DI |
| Adapter | `PasarelaLegadaAdapter` adapta la pasarela de centavos a `IPasarelaPago` |
| Bridge | `ComprobanteVenta`/`ComprobanteDevolucion` con implementaciones de `ISalidaDocumento` |
| Composite | `IElementoCatalogo`, `ProductoSimple`, `KitCompuesto`, `ComponenteKit` |
| Decorator | `AuditoriaPagoDecorator` rodea la llamada a la pasarela y registra su resultado |
| Facade | `ConfirmacionPedidoFacade` coordina validación, pago, existencias y evento |
| Flyweight | `CatalogoVisualFlyweightFactory` comparte metadatos; filas conservan datos extrínsecos |
| Proxy | `PagoAutorizadoProxy` aplica permiso antes de cobrar |
| Chain of Responsibility | Validadores enlazados en `CadenaConfirmacion` |
| Command | Comandos de operaciones e invocador con historial |
| Interpreter | `FiltroPedidosParser` y nodos del AST |
| Iterator | `IteradorFisico`, implementación de `IEnumerator<HojaCatalogo>` |
| Mediator | `FormularioPrincipalMediator` coordina actualizaciones de vistas |
| Memento | `HistorialBorrador`, estados de edición y deshacer/rehacer |
| Observer | `PublicadorEventos`, observers de auditoría, métricas y notificaciones, entrega durable |
| State | Objetos de estado del pedido y transiciones permitidas |
| Strategy | `IDescuento`, `Mayorista15`, `Vip10`, `SinDescuento` |
| Template Method | `ProcesarDevolucion` y políticas concretas por motivo |
| Visitor | `IVisitanteCatalogo`, `DesgloseFisicoVisitor`, `PesoLogisticoVisitor` |

El material recibido conserva pruebas inicialmente fallidas y sus posteriores correcciones, además de las ejecuciones finales aprobadas. Se revisaron las referencias de aceptación P01-P38 y se volvió a ejecutar toda la suite actual; las pruebas adicionales explican por qué una declaración previa de "sin pendientes" debe actualizarse.

## Historial y empaquetado

Rama revisada: `codex/completar-logimarket360`.

HEAD: `b494016a280e1b32cd5b8d4b97f530a0bd550fb3`.

`main` permanece en `1c6501d`, el cuarto commit original. Esto es coherente con desarrollar en una rama separada. Los ocho commits posteriores están en la rama de trabajo; no faltan por el hecho de que `main` conserve la versión anterior. No hay una cuota técnica de commits que justifique dividir cambios artificialmente.

El ZIP grande recibido es una copia de trabajo y contiene Git, resultados de compilación y archivos del entorno de desarrollo, como indicó el usuario. Es adecuado para esta revisión porque permite comprobar el historial. El ZIP limpio localizado en `artifacts` excluye `.git`, `.vs`, `bin`, `obj`, `tmp` y bases de datos; se verificaron su manifiesto y la correspondencia de sus fuentes. Tras las correcciones deberá regenerarse ese ZIP limpio para que incluya el nuevo HEAD y sus evidencias.

La publicación en GitHub y el informe académico formal siguen siendo las actividades aplazadas por el usuario.

## Cómo repetir las comprobaciones

Desde PowerShell en la carpeta del proyecto:

```powershell
cd "C:\Users\ASUS\source\repos\LogiMarket360"
dotnet restore .\LogiMarket360.sln --locked-mode
dotnet build .\LogiMarket360.sln --no-restore --configuration Release -warnaserror
dotnet test .\LogiMarket.Tests\LogiMarket.Tests.csproj --no-build --configuration Release --logger "trx;LogFileName=originales.trx" --results-directory .\TestResults\RevisionOriginales
```

Para las pruebas adicionales, descomprimir este paquete y situarse en su carpeta `Revision_LogiMarket360_2026-11-22`:

```powershell
dotnet test .\pruebas\RevisionProbes.csproj -p:LogiMarketRepo="C:\Users\ASUS\source\repos\LogiMarket360" --configuration Release --logger "trx;LogFileName=adicionales.trx" --results-directory .\ResultadosAdicionales
```

Las pruebas usan bases temporales mediante el fixture `Escenario` del propio proyecto. Referencian el proyecto Tests, pero esta invocación ejecuta los cinco casos de la revisión. La suite original se ejecuta con el comando anterior. Contra el código recibido se esperan los cinco fallos documentados; tras corregir el comportamiento deberán pasar sin debilitar sus expectativas. Si una corrección cambia las firmas públicas, se deberá adaptar el montaje de la prueba conservando el escenario y el resultado exigidos.

La prueba del carrito ordenado requiere además verificar los controles reales en Windows. El archivo `Prompt_Correcciones_Codex.txt` contiene instrucciones listas para continuar en el Codex local, con commits y verificaciones por corrección.
