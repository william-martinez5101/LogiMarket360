# Decisiones, riesgos y reparaciones

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
| Borrar/restablecer ventas al iniciar | Seed agrega faltantes, no actualiza operaciones ni stock existente | No habia seed -> relanzar despues de vender conserva datos; P01/P07/P10 |
| Permisos solo visuales dejan acceso directo al cobro | Permisos Application y Proxy externo al Decorator | No habia acceso -> CONSULTA/BODEGA no llegan al proveedor ni a su log; P22 |
| EF interpreta hijos con Guid asignado como filas existentes | ValueGeneratedNever para identidades asignadas por la aplicacion | **Fallo observado:** 8/10 integraciones fallaron; corregido a 10/10, luego suite completa. TRX integracion-inicial e integracion-reparada |
| Reducir lineas depende del orden accidental de una consulta SQL | Eliminar detalles por propiedad Orden, no por indice de lista | Hallazgo de revision; no se afirma un fallo previo reproducido. EdicionPersistidaTests conserva exactamente una linea y rechaza version obsoleta |
| Miles de nodos de accesibilidad demoran la herramienta de control | 1000 filas retenidas, presentacion paginada de 50 | Se observaron timeouts y titulo temporal No responde; diagnostico guardado. Proceso respondia y su hilo esperaba mensajes. Capturas37/38/39 prueban avance y salida de la nueva vista |
| La regla global de Git para .log excluye evidencia del ZIP basado en archivos versionados | Excepcion limitada a docs/evidencia/**/*.log | Revision del ZIP detecto seis logs ausentes; se incluyeron y se verificaron las rutas enlazadas desde la matriz |

## Consistencia del proveedor simulado

El proveedor utiliza un diario durable independiente de la transaccion comercial. Si el proveedor acepta y el commit local falla, no se registra un pago local parcial. Al reintentar **con la misma clave**, se recupera la referencia original y se completa la transaccion. El caso P20 lo ejecuta. Una referencia aceptada por un colaborador externo no puede deshacerse con rollback de SQLite: esta practica no exige transacciones distribuidas. La UI propone una clave estable por pedido. Un pedido ya pagado tiene ademas una restriccion unica por pedido aunque se intente otra clave.

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
