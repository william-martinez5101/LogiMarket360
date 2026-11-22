# Recorrido real en Windows

La segunda revision agrega el [recorrido R06 en dos procesos Windows y con reinicio](R06-2026-11-22/RESULTADOS.md): compromiso4/disponible0 tras rollback, B sin cargo y A recuperado una sola vez. Las capturas de los recorridos anteriores permanecen conservadas.

Este recorrido corresponde a la primera entrega y se conserva como registro historico. El [recorrido de correcciones del 22/11/2026](correcciones-2026-11-22/RESULTADOS.md) incorpora capturas nuevas del carrito ordenado y del cobro aceptado con fallo local, reinicios y otras claves/proveedor.

Ejecutado el 21 de noviembre de 2026, America/Guayaquil. Las marcas JSON/TRX se conservan en UTC. Aplicacion WinForms real, cuentas ficticias y base aislada `tmp/ui-verificacion/logimarket.db`. No se incluye ese archivo SQLite en el ZIP.

Pedido del recorrido: **df995c67-5056-4dbe-9afc-95a70328a5b6**. Pago: **SIM-714feb2f54e946b888cdff4d90ab4475**. Se utilizo admin.demo para venta/devolucion y bodega.demo para despacho/entrega.

| Operacion observada | Resultado real | Captura o registro |
| --- | --- | --- |
| Acceso y panel inicial | Login ficticio, cero operaciones iniciales | ui/01-acceso.png, ui/02-panel-inicial.png |
| Catalogo y kit anidado | Cinco productos oficiales; kits 50/60; teletrabajo P01/P02/P03, peso1.00kg | ui/03-catalogo-inicial.png, ui/04-kit-anidado.png |
| Clientes | Ana VIP, Luis NORMAL, Carmen MAYORISTA; nombre vacio rechazado y Cliente UI creado | ui/05-clientes.png, ui/06-cliente-validacion.png, ui/07-cliente-registrado.png |
| Carrito y edicion | Ana, teletrabajo1, estandar: 60-6+5=59; deshacer quita linea y rehacer la restaura | ui/08-carrito-ana59.png, ui/09-deshacer.png, ui/10-rehacer.png |
| Validar y simular rechazo de pago | VALIDADO; rechazo conserva estado sin cobro exitoso | ui/11-pedido-validado.png, ui/12-cobro-rechazado.png |
| Aprobar cobro con notificador fallido | PAGADO59, stock7/4/19; evento PENDIENTE, un intento | ui/13-cobro-aprobado59.png, ui/14-stock-tras-cobro.png, ui/15-outbox-pendiente.png |
| Cerrar proceso y reabrir misma base | Antes: un pago, tres movimientos, cero avisos; despues: mismo pedido/pago y un aviso recuperado | ui/16-datos-antes-reinicio.json, ui/17-reinicio-persistencia.png |
| Despachar y entregar como BODEGA | Guia ESTANDAR, plazo3, DESPACHADO -> ENTREGADO; boton de cobro deshabilitado | ui/18-despacho-bodega.png, ui/19-entrega-bodega.png, ui/20-bodega-cobro-bloqueado.png |
| Cerrar sesion y volver a entrar | Regresa al acceso; no conserva la identidad BODEGA para otra sesion | ui/21-cierre-sesion.png |
| Devolucion DEFECTO y repeticion | Reembolso54 sin flete; segunda solicitud rechazada; stock8/5/20 | ui/22-devolucion54.png, ui/23-devolucion-repetida-rechazada.png, ui/24-inventario-reintegrado.png |
| Historial y reportes | Cinco transiciones con actor y fecha; cobro59 y devolucion54, neto5 | ui/25-historial-completo.png, ui/26-reportes.png |
| Bridge, cuatro combinaciones | Dos vistas previas visibles y dos TXT leidos del disco | ui/28-comprobante-venta-visible.png, ui/29-txt-venta.png, ui/31-comprobante-devolucion-visible.png, ui/32-txt-devolucion.png; TXT adjuntos en ui/ |
| Flyweight y paginacion | 1000 filas, cinco metadatos, ReferenceEquals=True; 58104 vs104232 bytes asignados en esa ejecucion; pagina2 muestra51-100 | ui/37-flyweight-pagina1.png, ui/38-flyweight-pagina2.png, ui/39-navegacion-tras-flyweight.png |
| Iterator/Visitor desde boton | P01/P02/P03x1, 1.00kg, precio60 | ui/40-iterator-visitor-real.png |
| Outbox y avisos finales | Tres eventos PROCESADO; pago recuperado en intento2; tres avisos unicos | ui/41-outbox-recuperado.png, ui/42-avisos-unicos.png |
| Reinicio posterior a devolucion | Se conservan ENTREGADO, cobro59, reembolso54 y tres avisos | ui/36-segundo-reinicio.png |
| Integridad final de SQLite | Un pago, una devolucion, seis movimientos, tres eventos, nueve marcas de suscriptor, tres avisos. integrity_check=ok y foreign_key_check vacio | ui/43-datos-finales.json, ui/44-traza-real.jsonl |

Se guardaron tambien los arboles de accesibilidad y fechas junto a las capturas, como `.txt`. Las capturas son originales sin retoques. En la demostracion inicial de 1000 filas la inspeccion completa de accesibilidad tuvo timeouts; se conserva la evidencia y `ui/diagnostico-flyweight.txt`. Tras paginar a50 se verificaron avance y salida normal, sin nuevas operaciones comerciales ni duplicados. La herramienta de control se reinicio durante el diagnostico.

## Comprobaciones pendientes

No queda pendiente una etapa del recorrido de venta hasta devolucion ni su persistencia. La compilacion desde el ZIP limpio se registra por separado en ACEPTACION.md. El informe academico formal y GitHub se mantienen para despues, como solicito el usuario.
