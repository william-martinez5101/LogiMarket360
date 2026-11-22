# Segunda revisión independiente de LogiMarket360

Fecha: 22 de noviembre de 2026. Entrada: `LogiMarket360(1).zip`, copia de la carpeta real `C:\Users\ASUS\source\repos\LogiMarket360`.

## Resultado

Las correcciones de los cinco puntos anteriores están incorporadas y los cinco casos externos que fallaban ahora pasan. La solución completa compila con cero errores y cero advertencias; las 149 pruebas del proyecto pasan, incluyendo las 109 originales y 40 regresiones nuevas. Se conservaron los doce commits recibidos y hay ocho posteriores: veinte en total.

Queda un escenario de recuperación que esta revisión reprodujo con una prueba nueva: un pedido con cobro aceptado pero sin commit local puede perder la disponibilidad de sus unidades cuando se confirma otra venta. El primer pedido conserva el cargo del proveedor y queda bloqueado por stock, mientras Cancelar exige precisamente confirmarlo. Conviene resolver este punto antes de cerrar la revisión funcional.

La protección contra el cobro duplicado del mismo pedido sí funciona en los casos anteriores. El hallazgo nuevo trata de dos pedidos distintos y de las existencias necesarias para conciliar el primero.

## Verificación independiente

| Comprobación | Resultado | Evidencia propia |
| --- | --- | --- |
| Restauración bloqueada y compilación Release de los cinco proyectos, advertencias como errores | Correcta; 0 errores y 0 advertencias | `evidencia/restore-solucion.log`, `evidencia/build-solucion.log` |
| Suite del proyecto | 149 aprobadas, 0 fallidas y 0 omitidas | `evidencia/suite-corregida.trx` |
| Casos externos de la primera revisión, con su fuente conservada | 5 aprobados, 0 fallidos | `evidencia/adjuntas-corregidas.trx` |
| Comparación de nombres y cantidad de casos originales | Las 109 originales presentes; 40 casos nuevos | `evidencia/comparacion-pruebas.json` |
| Prueba nueva de cobro pendiente y venta posterior | 1 ejecutada y fallida | `evidencia/stock-pendiente.trx` y log correspondiente |
| Integridad del ZIP limpio de correcciones | 458 archivos del manifiesto con hashes correctos; sin diferencias de contenido respecto a la carpeta recibida al normalizar CRLF/LF | `evidencia/integridad-paquete.json` |

Se utilizó SDK .NET 8.0.420, runtime .NET 8.0.26, sobre Linux. La compilación WinForms fue cruzada con `EnableWindowsTargeting=true`. Se inspeccionaron las capturas y registros Windows suministrados; no se ejecutó aquí la interfaz Windows.

Las verificaciones se hicieron sobre una copia extraída. El código productivo y el historial recibidos se conservaron; la prueba nueva está en un proyecto separado y usa SQLite temporal mediante el fixture del proyecto.

## Estado de las cinco correcciones anteriores

| Punto | Comprobación | Estado de esta revisión |
| --- | --- | --- |
| R01 Cobro duplicado tras rollback y cambio de clave | Intentos ligados al pedido, recuperación de la referencia original, propiedad de alias, cambio de proveedor y reinicio; pasa el caso externo original | Corregido para los escenarios anteriores; ampliar con R06 |
| R02 Seed que restauraba productos eliminados | Marca transaccional, adopción de bases anteriores, bajas/renombrados conservados y prueba de inicialización interrumpida | Corregido |
| R03 Carrito ordenado | `DataBoundItem` de tipo `FilaLineaCarrito` conserva el ordinal del modelo durante el render; pruebas con duplicados y capturas del control Windows | Corregido en código y respaldado por la evidencia suministrada |
| R04 Instantáneas de precio/composición fabricadas | Las líneas nuevas se capturan del catálogo en Application; las históricas legítimas del borrador se conservan; pagos/devoluciones históricos no se recalculan | Corregido |
| R05 Sintaxis del Interpreter | Tokens tipados y posiciones de error; los dos filtros inválidos anteriores se rechazan; nombres Unicode/comillas y precedencia conservados | Corregido |

Las pruebas originales no se sustituyeron por expectativas más débiles: en los archivos originales de pruebas solo cambió el cableado de `Escenario` para incorporar el nuevo puerto; las regresiones son archivos nuevos. La comparación por casos confirma que las 109 siguen ejecutándose.

La evidencia Windows suministrada muestra, entre otros pasos, P04/P01/P01 con cantidades 1/1/2 y total 95.00 después de cambiar el duplicado correcto; y la recuperación del cobro de Ana por 59.00 con clave distinta y proveedor seleccionado ALTERNATIVO, devolviendo la referencia del proveedor original LEGADO. También se conservaron registros de reinicios y consultas de SQLite. Estas observaciones corresponden a los archivos recibidos, además de las verificaciones automáticas independientes anteriores.

## R06. Un cobro aceptado pendiente pierde disponibilidad frente a otra venta

Prioridad: alta dentro de la recuperación del cobro. Reproducido ejecutando Application, Infrastructure y SQLite reales sobre datos temporales.

### Secuencia reproducida

1. Base inicial oficial: P04 tiene cuatro unidades, precio 40.00.
2. Crear y validar dos pedidos distintos, A y B, para Luis NORMAL, cada uno con P04 por cuatro unidades y entrega ESTÁNDAR. Cada total es 165.00.
3. Activar `FallarPersistencia` y confirmar A con `pendiente-A`. El proveedor acepta 165.00; el commit local falla. La base conserva A VALIDADO y el stock sigue en cuatro.
4. Desactivar el fallo y confirmar B con `venta-B`. B se cobra y confirma; consume las cuatro unidades.
5. Abrir otra instancia sobre la misma base y reintentar A con `recuperar-A`.
6. Intentar cancelar A.

### Resultado observado

| Dato | Valor real |
| --- | --- |
| Venta B | Aprobada |
| Recuperación A | `STOCK: Stock insuficiente para P04: requiere 4, disponible 0.` |
| Estado A | VALIDADO |
| Pagos locales A | 0 |
| Pagos locales B | 1 |
| Stock P04 | 0 |
| Aceptaciones del proveedor | 2, una por cada pedido distinto |
| Importe aceptado acumulado | 330.00 |
| Cancelar A | Rechazado: hay un cobro aceptado y se exige conciliar confirmando |

El proveedor conserva el cobro de A por 165.00, pero A no puede confirmarse con las existencias disponibles ni cancelarse por la vía actual. No hay una reserva de unidades para esa aceptación pendiente. La segunda venta se autoriza usando el mismo stock disponible que necesitará la conciliación del primer pedido.

La prueba `AceptacionPendiente_NoDebePerderSusUnidadesPorOtraVenta` falla al exigir que la recuperación de A sea posible. Es una prueba adicional de esta segunda revisión; no forma parte de las 149 ni de las cinco adjuntas anteriores.

### Ubicaciones relevantes

- `LogiMarket.Application/Pedidos/ConfirmacionPedidoFacade.cs`, líneas 29-33: se comprueba stock actual y se rechaza antes de recuperar el intento aceptado.
- El mismo archivo, líneas 37-45: el descuento de stock queda dentro de la transacción que revierte cuando falla el guardado.
- `LogiMarket.Infrastructure/Pagos/DiarioCobrosSimulados.cs`, `Reserva`/`PrepararAsync`: la reserva durable conserva pedido, importe y claves; no protege cantidades por SKU.
- `LogiMarket.Application/Pedidos/PedidosServicio.cs`, líneas 48-55: Cancelar rechaza una aceptación pendiente y pide confirmar.

### Corrección recomendada

Proteger durablemente la disponibilidad comprometida por un cobro aceptado pendiente, de modo que otra venta no pueda consumir esas unidades antes de su conciliación. Una reserva persistente de existencias, o una comprobación equivalente bajo el bloqueo transaccional existente, debe sobrevivir al rollback comercial y a los reinicios.

La confirmación de A debe reconocer su propia reserva sin restarla dos veces. Las reservas deben liberarse o consumirse exactamente una vez según el resultado del proveedor y del commit; los rechazos y las cancelaciones permitidas deben conservar una disponibilidad correcta. Mantener el stock no negativo, un cobro/pago por pedido, movimientos únicos y Outbox sin duplicados. No basta con omitir la validación de stock al reintentar.

El criterio de la prueba adjunta favorece conservar las unidades de A y rechazar B cuando no queden unidades libres. Si se propone otra política de conciliación, deberá dejar un resultado comercial completo, trazable y conforme con el PDF; el cargo de A no debe quedar sin salida detrás de un error de stock.

### Verificaciones mínimas tras corregir

- El caso reproducido: A aceptado con rollback; B intenta consumir las mismas cuatro unidades; reinicio; A recupera su referencia original y queda PAGADO una sola vez.
- Reservas por SKU agregado en kits y líneas repetidas; ventas de otros SKU pueden continuar con disponibilidad suficiente.
- Rechazo del proveedor y reintentos: liberar reservas que correspondan sin filtrar stock ni reintroducir cobros duplicados.
- Dos instancias concurrentes y reinicios entre aceptación y commit.
- Conservar 149 pruebas originales/ampliadas y las cinco externas aprobadas; agregar la nueva regresión y evidencia real.

## Historial y paquete recibido

Rama: `codex/completar-logimarket360`.

HEAD: `b2754b0013e000bd8e89d8bc9616d96a15c6e31b`.

Commits posteriores a la primera revisión:

| Commit | Cambio |
| --- | --- |
| a054cdb | Recuperar un solo cobro por pedido tras rollback |
| ed26994 | Conservar bajas y ediciones tras reiniciar |
| e207608 | Capturar nuevas instantáneas desde el catálogo |
| 14639f2 | Rechazar operadores como valores de cliente |
| c6d9f5f | Respetar la línea enlazada al ordenar el carrito |
| 53583a7 | Conciliación de aceptaciones antiguas sin identidad de pedido |
| a2bcd3f | Pruebas y evidencia de correcciones en Windows |
| b2754b0 | Comprobación de copia limpia con SDK 8, migraciones y seed |

Se compararon los 458 archivos seguidos con los objetos de HEAD: ninguno falta y no hay diferencias de contenido salvo finales de línea CRLF/LF. Los doce commits previos están conservados; `main` permanece en el punto inicial de cuatro commits, mientras el desarrollo continúa en la rama indicada.

ZIP limpio actual: `artifacts/APE04_LogiMarket360_Correcciones_2026-11-22.zip`, 6,675,899 bytes. Su SHA-256 coincide con el archivo lateral, su manifiesto apunta a b2754b0 y los 458 archivos coinciden con la carpeta recibida. No contiene `.git`, `.vs`, `bin`, `obj`, `tmp` ni bases de uso. Esta revisión distingue ese paquete limpio del ZIP grande de trabajo que el usuario entregó para analizar.

El bloqueo seguro de nuevas ventas cuando existe un cobro huérfano de una versión anterior sin `PedidoId` está documentado. Es una limitación de compatibilidad que requiere conciliación manual; el caso R06 usa el formato nuevo con identidad de pedido y es independiente de esa limitación.

Tras corregir R06 conviene regenerar el paquete limpio y sus registros. GitHub y el informe académico formal siguen aplazados.

## Repetir la nueva prueba

Descomprimir este paquete y abrir PowerShell en `Segunda_Revision_LogiMarket360_2026-11-22`:

```powershell
dotnet test .\prueba_nueva\RevisionProbes.csproj -p:LogiMarketRepo="C:\Users\ASUS\source\repos\LogiMarket360" --configuration Release --logger "trx;LogFileName=stock-pendiente.trx" --results-directory .\ResultadosR06
```

El proyecto de prueba referencia el fixture existente y crea una base temporal. Contra el HEAD recibido se espera el fallo descrito. El archivo `Prompt_Correccion_R06_Codex.txt` contiene una petición puntual para continuar en el Codex local con sus commits y verificaciones.
