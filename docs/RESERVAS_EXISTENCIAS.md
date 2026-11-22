# Existencias comprometidas por aceptaciones pendientes (R06)

Una aceptacion del proveedor con PedidoId y las instantaneas ya persistidas del pedido VALIDADO constituyen el compromiso durable de unidades. El diario se escribe fuera de la transaccion comercial que puede revertirse; las lineas de un VALIDADO no admiten edicion ni cancelacion si hay una aceptacion. No hace falta una segunda copia de cantidades ni una migracion nueva para mantener esta reserva equivalente.

Application calcula, por SKU, la suma de `Pedido.Fisicos()` de aceptaciones que aun no tienen pago local. Esa expansion agrupa productos de kits anidados y lineas repetidas, con cantidades comprobadas. Siempre utiliza el pedido persistido, nunca la composicion o el precio actual del catalogo. Un pago local se verifica contra su aceptacion y deja de formar parte del compromiso: ese mismo commit ya desconto el stock fisico.

Para otra venta: **disponible = max(0, fisico - comprometido)**. Para conciliar el propietario: se excluye solo su propio compromiso y se sigue comprobando su cantidad contra el stock fisico y los compromisos de los demas. La validacion no se omite. Un pedido PAGADO devuelve primero su pago original, sin volver a descontar ni consumir unidades.

En el escenario oficial P04 fisico4, A aceptado por4 y sin commit: quedan4 comprometidas y0 libres. B por4 se rechaza antes de llamar al proveedor. Tras reiniciar, A ve sus4 propias disponibles y confirma exactamente una vez: fisico0, comprometido0, un pago/movimiento/evento. Si A compromete solo2, las otras2 y los otros SKU siguen vendiendose.

## Concurrencia y ciclo de vida

- Confirmacion y ajustes negativos calculan disponibilidad despues de adquirir BEGIN IMMEDIATE, igual que el cobro y el commit. Ninguna otra venta ni ajuste puede intercalar un descuento entre comprobacion y aceptacion/commit, aun desde otra instancia.
- Durante la llamada al proveedor protege el bloqueo SQLite existente. Si el proveedor rechaza, no existe aceptacion ni compromiso; el rollback deja las unidades libres y la cancelacion permitida sigue funcionando. Una clave de intento queda ligada al pedido como antes.
- Si el proveedor acepta y falla el commit, el diario y el pedido VALIDADO conservan el compromiso tras rollback/reinicio. Reintentar con otras claves/proveedor recupera la referencia original y no multiplica el compromiso.
- Al confirmar el pago, pago/stock/movimientos/estado/evento se registran en la misma transaccion. La presencia de ese pago consume el compromiso en la siguiente consulta; no hay una liberacion adicional que pueda perderse si el proceso termina despues del commit.
- Una devolucion reintegra stock fisico segun el flujo existente. La aceptacion antigua sigue teniendo pago local y no vuelve a reservarlo. Las entregas Outbox mantienen su deduplicacion por evento/suscriptor.
- Las consultas del diario comparten su bloqueo de archivo con los escritores. La espera breve es acotada a cinco segundos y admite cancelacion; una lectura de otra ventana no produce inmediatamente un error de archivo ocupado.

## Limites y conservacion

La fuente de verdad del stock sigue siendo SQLite, con sus restricciones y control de concurrencia. La aceptacion durable y sus archivos de intentos siguen en Infrastructure; Application solo conoce IIntentosCobro y DTO de disponibilidad. WinForms configura las dependencias. Domain no cambia ni incorpora SQLite/EF.

Se conserva el directorio completo de cada base junto a sus diarios. Una base nueva aplica la migracion original y el seed idempotente existente. La reserva equivalente tambien reconoce aceptaciones anteriores con PedidoId y pedido VALIDADO compatible. No reconstruye stock consumido antes de esta correccion: datos ya sobrecomprometidos o aceptaciones sin pedido compatible requieren conciliacion, sin inventar unidades ni omitir restricciones. El caso antiguo sin PedidoId conserva su bloqueo seguro previo de nuevas ventas.

ConsultasApp entrega existencias fisicas, comprometidas y disponibles para las vistas. La disponibilidad visual es informativa; cada accion vuelve a comprobarla en Application bajo la transaccion de escritura. Las pruebas y la demostracion R06 usan bases temporales; la base normal del usuario se conserva.
