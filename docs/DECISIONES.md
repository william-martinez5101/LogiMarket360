# Decisiones, riesgos y reparaciones

## Segunda revision: disponibilidad comprometida R06

La deduplicacion por pedido de R01 funcionaba, pero la prueba R06 reprodujo dos aceptaciones330 para pedidos distintos y solo un pago local: A aceptado con rollback perdia sus unidades cuando B las vendia. Se corrigio con una reserva equivalente derivada de la aceptacion durable y las lineas inmutables de VALIDADO. La validacion bajo BEGIN IMMEDIATE descuenta compromisos de otros pedidos y conserva las unidades propias para conciliar. La presencia del pago local consume el compromiso en el mismo commit que desconto stock; no requiere liberar una segunda tabla despues del commit. Se aplican las mismas disponibilidades a validacion y ajustes negativos.

Se comprueban cantidades libres, otros SKU, kits/lineas repetidas, rechazos, cancelacion impaga, dos instancias concurrentes, dos rollback del mismo intento, reinicios, Outbox y acceso concurrente al diario. El control de archivo espera brevemente y respeta cancelacion. Los registros originales de R01-R05 se conservan. [Diseno y limites](RESERVAS_EXISTENCIAS.md); [evidencia R06](evidencia/R06-2026-11-22/RESULTADOS.md).

## Registro honesto de construccion

El punto de partida 1c6501d tenia clientes, productos y Composite con 52 pruebas. Los servicios y la interfaz funcional aun no existian. La siguiente tabla distingue riesgos de diseño tratados al construir las funciones de fallos efectivamente observados. No se introdujeron defectos ni se crearon commits vacios para simular evolucion.

| Riesgo real | Tratamiento / cambio verificable | Antes y despues / comprobacion |
| --- | --- | --- |
| Persistencia acoplada al nucleo | Puertos de Application y adaptadores Infrastructure | Base inicial sin persistencia -> EF8 aislado; P38 y csproj |
| Mutacion del catalogo altera una venta anterior | Instantanea profunda y filas fisicas normalizadas | Catalogo mutable existente -> snapshots de PedidoBuilder; SnapshotConservaPrecioYComposicionTrasEditarCatalogo |
| Cantidades de kits y SKU repetidos permiten subestimar stock | Expansion y agrupacion por SKU con checked | Composite inicial calculaba precio -> cantidad final y movimiento unico; P05/P18/P27/P34 y SkusRepetidosSeAgrupan |
| Reglas de estados dispersas o setters que salten el ciclo | Agregado con seis colaboradores State e historial | No habia pedidos -> acciones protegidas; P08/P09/P31 |
| Acumular promociones o elegir por etiqueta MAYORISTA | IDescuento con prioridad explicita y calculadora estable | No habia calculadora -> Mayorista por unidades, luego VIP; P02/P03/P04/P32 |
| Reintento posterior a PAGADO duplica cobro | Buscar clave antes del estado; indices unicos y diario de proveedor | No habia confirmacion -> resultado original tras reinicio; P07/P20 |
| Detectar sobreventa despues de cobrar a ambos | BEGIN IMMEDIATE antes de lectura y proveedor, token de stock | No habia concurrencia -> dos conexiones, un cobro aprobado; P36 |
| Fallo local deja pago/stock/evento parciales | Unit of Work y transaccion real | No habia unidad transaccional -> fallo inducido antes de commit revierte todos los datos locales; P06/P20 |
| Reintento de un suscriptor repite los otros | Marcas durables por evento/suscriptor en la misma transaccion que su efecto | No habia Outbox -> fallo notificador, reinicio y un efecto por suscriptor; P30/P37 |
| Clones y undo comparten colecciones | Copia profunda de arboles; historial maximo cinco, bloqueo tras registro valido | No habia borrador -> aislamiento comprobado; P14/P29 |
| Borrar/restablecer ventas al iniciar | Seed inicial transaccional con marca V1; adopcion de bases existentes sin reponer faltantes | P01/P07/P10 y regresiones R02: bajas P04/kit, renombrado de Ana, ediciones e interrupcion real |
| Permisos solo visuales dejan acceso directo al cobro | Permisos Application y Proxy externo al Decorator | No habia acceso -> CONSULTA/BODEGA no llegan al proveedor ni a su log; P22 |
| EF interpreta hijos con Guid asignado como filas existentes | ValueGeneratedNever para identidades asignadas por la aplicacion | **Fallo observado:** 8/10 integraciones fallaron; corregido a 10/10, luego suite completa. TRX integracion-inicial e integracion-reparada |
| Reducir lineas depende del orden accidental de una consulta SQL | Eliminar detalles por propiedad Orden, no por indice de lista | Hallazgo de revision; no se afirma un fallo previo reproducido. EdicionPersistidaTests conserva exactamente una linea y rechaza version obsoleta |
| Miles de nodos de accesibilidad demoran la herramienta de control | 1000 filas retenidas, presentacion paginada de 50 | Se observaron timeouts y titulo temporal No responde; diagnostico guardado. Proceso respondia y su hilo esperaba mensajes. Capturas37/38/39 prueban avance y salida de la nueva vista |
| La regla global de Git para .log excluye evidencia del ZIP basado en archivos versionados | Excepcion limitada a docs/evidencia/**/*.log | Revision del ZIP detecto seis logs ausentes; se incluyeron y se verificaron las rutas enlazadas desde la matriz |

## Consistencia del proveedor simulado

La revision independiente reprodujo un defecto real en la entrega anterior: P20 cubria solo la misma clave; otra clave despues de rollback producia un segundo cargo. `IIntentosCobro` ahora reserva cada clave a un pedido y conserva su importe en `intentos-cobro.json`; el diario de aceptaciones guarda tambien PedidoId. Ambos usan el mismo bloqueo de archivo, escritura temporal, Flush(true) y reemplazo atomico, fuera de la transaccion comercial. Application busca una aceptacion durable antes de volver a invocar la pasarela. Una nueva clave del mismo pedido recupera referencia y proveedor originales; una clave reservada a otro pedido se rechaza. Los pagos ya confirmados se recuperan antes de evaluar de nuevo el estado o el stock. No se permite cancelar un pedido con aceptacion pendiente.

El bloqueo SQLite BEGIN IMMEDIATE serializa confirmaciones de la misma base, tambien entre instancias. Los indices unicos y la transaccion conservan un pago, un movimiento por SKU y un evento; Outbox deduplica cada efecto. R01 comprueba ambos proveedores, cambios de clave/proveedor, reinicio, concurrencia, propiedad de alias, auditoria del error y recuperacion. El fallo de demostracion afecta solo el guardado de un pago nuevo, despues de la aceptacion: el registro COMMAND INICIO/ERROR sigue funcionando. Se verifico tambien desde la UI real.

Para compatibilidad, un diario anterior sin PedidoId se puede leer. Si su referencia ya esta en Pagos, no bloquea nuevas ventas. Si queda una aceptacion sin pago local correspondiente, no existe informacion suficiente para atribuirla por pedido: se bloquean nuevos cargos con un error de conciliacion, sin inventar asociaciones por importe. Conserve siempre el directorio completo de base y diarios. No hay transacciones distribuidas ni integracion bancaria; la garantia corresponde a estos proveedores locales simulados.

## Correcciones adicionales de la revision

- El seed usa una marca de version con PK fija en Auditorias, creada junto con todos los datos en la transaccion existente. No requiere modificar la migracion original ni las bases de uso. Una base anterior con datos se adopta intacta; no se reconstruyen entidades por SKU/nombre ausente. Un trigger de prueba interrumpe realmente la insercion de kits y comprueba rollback completo y reintento.
- CarritoVista resuelve la posicion del modelo desde FilaLineaCarrito enlazada, conservada al ordenar la proyeccion; no usa CurrentRow.Index ni busca solo por SKU. La ordinal identifica cada ocurrencia durante el render y se regenera al editar, quitar o restaurar un memento. Se verificaron orden ascendente/descendente y duplicados en Windows.
- CapturadorLineasPedido considera los datos del llamador una solicitud. Una linea nueva se captura del catalogo actual por codigo/cantidad. Solo admite historicas de ese borrador previamente persistido que coincidan en todos los valores y el arbol completo, consumiendo una ocurrencia por linea. La copia conservada procede del servidor. Las nuevas ocurrencias y las manipuladas se capturan del catalogo; los pagos/devoluciones historicos no se recalculan.
- El lexer distingue identificador, texto, numero, operador, parentesis y conector. CLIENTE admite identificador o texto no vacio; '=' y '>=' solo se admiten como texto de cliente si estan entre comillas. Los errores senalan la posicion real del token. Se conservan AST, AND antes de OR, parentesis y consultas sin SQL concatenado.

## SOLID y alcance

- SRP: dominio calcula y protege reglas; servicios orquestan; repositorios mapean; vistas presentan. No hay SQL/EF en Domain/Application ni tarifas codificadas en formularios.
- OCP: promociones, creadores de avisos, salidas y observadores se registran como colaboradores. P32 agrega una promocion sin modificar la calculadora.
- LSP: los dos proveedores comparten validacion de importe/clave, cancelacion y resultado; P16/P38 ejecutan sus contratos.
- ISP: IPasarelaPago, IHashContrasena, ISalidaDocumento y observadores tienen contratos separados; las vistas no reciben DbContext.
- DIP: Application declara repositorios y servicios; Infrastructure los implementa. Program compone DI. Los comandos criticos se separan de las consultas (CQRS basico).

## Limites comprobables

- Aplicacion local de demostracion: no hay banco, correo, SMS, reembolso externo ni factura tributaria. El reembolso simulado es el registro unico durable de Devoluciones.
- SQLite serializa escritores: el bloqueo se conserva durante el cobro simulado. Es deliberado para impedir dos aprobaciones sobre la ultima unidad. No se pretende rendimiento de un sistema distribuido.
- ConsultasApp carga el estado operativo para las vistas. Los conjuntos de aceptacion son pequeños; no se ha medido rendimiento con millones de pedidos. Flyweight mide asignaciones del hilo para datos equivalentes, no memoria residente ni porcentajes universales de ahorro.
- La inspeccion completa de accesibilidad de tablas grandes supero el limite de la herramienta de control. La tabla paginada funciono por captura/coordenadas y se comprobo el cambio 1-50 -> 51-100 y retorno a Auditoria. No se oculta la captura inicial ni el diagnostico.
- Las fechas de devolucion se comparan por fecha UTC con limite inclusivo; los registros se muestran en la zona local del equipo.
- No se han realizado publicacion GitHub ni informe academico PDF: ambos fueron expresamente aplazados. Los diagramas, matrices y capturas quedan como insumos tecnicos para esa etapa.
