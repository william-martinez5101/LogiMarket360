# Continuidad de LogiMarket 360

Fecha del traspaso: 21 de noviembre de 2026. Usuario: William Martinez. Materia: Patrones de Software, Universidad Tecnica de Ambato.

## 1. Objetivo y autorizacion del usuario

El usuario autoriza continuar autonomamente la construccion completa del programa existente, modificar sus archivos, compilar, ejecutar pruebas, corregir problemas y registrar commits reales a medida que se avanza. Quiere agilizar el trabajo mediante Codex local en su Windows. La construccion habia sido guiada paso a paso en Visual Studio, pero ahora desea que el agente ejecute las tareas de desarrollo.

Trabajar sobre la carpeta EXISTENTE:

```text
C:\Users\ASUS\source\repos\LogiMarket360
```

Conservar su historial Git y su trabajo. La publicacion en GitHub y el informe academico formal se haran despues de terminar el programa. No crear un remoto, hacer push ni publicar por iniciativa propia en esta etapa. El README tecnico, las instrucciones de ejecucion y los registros reales de pruebas y decisiones son necesarios durante la construccion y si entran en esta etapa.

No hay una cuota obligatoria de commits. Se estimo un total aproximado de 30 a 35 para organizar el trabajo, pero deben responder a avances reales. Agrupar cambios relacionados, comprobarlos y hacer el commit correspondiente. No crear commits vacios, reconstruir un historial ficticio al final ni inventar defectos para cumplir una cantidad. Agregar commits de correccion cuando sean necesarios.

Responder y dar avances en espanol. Los comentarios de codigo y los mensajes escritos en codigo se han venido redactando sin tildes. Usar nombres claros y explicar decisiones relevantes. No detenerse para pedir confirmaciones de rutina sobre cambios reversibles ya autorizados. Si falta un dato imprescindible, preguntar y continuar con lo que sea independiente.

## 2. Fuentes y prioridad

Leer COMPLETO `GUIA_APE_04.pdf`, incluido el caso, las tablas, los 12 wireframes, los 38 casos de aceptacion, las reglas de calidad y los entregables. Son 20 paginas. Este archivo de continuidad es un resumen de trabajo, no un sustituto del PDF.

Archivos de esta carpeta:

- `GUIA_APE_04.pdf`: enunciado original y fuente normativa de la actividad.
- `GUIA_APE_04_TEXTO.txt`: extraccion textual de las 20 paginas para facilitar busquedas; las imagenes y la maquetacion deben consultarse en el PDF.
- `ANALISIS_MATERIA.md`: analisis anterior de los 19 materiales de la materia, incluidos GoF, Clean Architecture, SOLID, Clean Code, antipatrones y practicas. Es material de apoyo, no una prueba de implementacion.
- `CONTEXTO_LOGIMARKET360.md`: este documento con las decisiones y el estado del traspaso.

Las instrucciones explicitas del usuario sobre el flujo de trabajo tienen prioridad sobre este resumen. Las reglas numericas y los requisitos de implementacion deben contrastarse con el PDF. Ante una diferencia real, explicar y resolverla; no cambiar cifras para ajustarlas a un wireframe ilustrativo.

## 3. Estado existente, comprobado por capturas del usuario

La siguiente informacion fue observada en capturas de Visual Studio y PowerShell del usuario. El agente local debe verificar el repositorio real antes de modificarlo.

Solucion: `LogiMarket360.slnx`.

| Proyecto | Tipo y destino | Referencias propias configuradas |
| --- | --- | --- |
| LogiMarket.Domain | Biblioteca C#, net8.0 | Ninguna |
| LogiMarket.Application | Biblioteca C#, net8.0 | Domain |
| LogiMarket.Infrastructure | Biblioteca C#, net8.0 | Application, Domain |
| LogiMarket.WinForms | Windows Forms, net8.0-windows | Application, Infrastructure |
| LogiMarket.Tests | xUnit, net8.0 | Domain, Application, Infrastructure |

Infrastructure se referencia desde WinForms para la composicion de dependencias. Las reglas de negocio no se deben poner en los formularios. El usuario usa un SDK 10.0.404, que ya compilo los destinos .NET 8. El banner del SDK no significa que se haya cambiado el TargetFramework. Mantener .NET 8 y EF Core 8 como pide la guia. Conservar la solucion; si hace falta una `.sln` adicional para compatibilidad con herramientas, hacerlo de forma explicita y comprobarla.

Historial observado, del mas antiguo al mas reciente:

| Hash corto | Mensaje |
| --- | --- |
| 88393c6 | chore: inicializar solucion y proyectos base de LogiMarket360 |
| ce88f5c | feat(domain): agregar clientes con invariantes y pruebas |
| 3d747f8 | feat(domain): agregar productos y reglas de inventario con pruebas |
| 1c6501d | feat(domain): incorporar kits anidados con Composite y validacion de ciclos |

Ultimo estado observado: rama `main`, HEAD `1c6501d`, `nothing to commit, working tree clean`. Agregar los archivos de continuidad generara cambios nuevos esperables. No atribuirlos a un error del usuario.

La solucion compilo con:

```powershell
dotnet build .\LogiMarket360.slnx
dotnet test .\LogiMarket.Tests\LogiMarket.Tests.csproj --no-build
```

Resultado real observado: 52 casos aprobados, 0 fallidos, 0 omitidos. Son 11 de clientes, 22 de productos y 19 de kits. El formulario inicial Form1 tambien se ejecuto vacio. Esto NO demuestra todavia los flujos de venta ni las 38 aceptaciones: aun faltan Application, Infrastructure, la interfaz funcional y las pruebas de integracion.

### Archivos del dominio ya construidos

En `LogiMarket.Domain/Clientes`:

- `CategoriaCliente.cs`: enum Normal=0, Vip=1, Mayorista=2.
- `Cliente.cs`: Id Guid, Nombre, Categoria y Activo con setters privados. Constructor publico valido; constructor privado reservado para persistencia. Metodos ActualizarDatos, Activar y Desactivar. Valida nombre no vacio y categoria definida; valida toda la actualizacion antes de modificar propiedades.

En `LogiMarket.Domain/Catalogo`:

- `ProductoSimple.cs`: Id, Sku, Nombre, Precio decimal, PesoKg decimal, Stock int; setters privados. Constructor `(string sku, string nombre, decimal precio, decimal pesoKg, int stockInicial)`. Normaliza SKU y nombre. Precio positivo, peso y stock no negativos. ActualizarDatos valida antes de modificar. DescontarStock valida cantidad positiva y disponibilidad. ReintegrarStock valida cantidad y usa suma checked. Implementa IElementoCatalogo, Codigo devuelve Sku, CalcularPrecio devuelve Precio.
- `IElementoCatalogo.cs`: Id, Codigo, Nombre y `decimal CalcularPrecio()`.
- `ComponenteKit.cs`: Elemento IElementoCatalogo y Cantidad, solo lectura; constructor internal valida referencia y cantidad positiva.
- `KitCompuesto.cs`: Id, Codigo, Nombre, lista privada de componentes y vista AsReadOnly; constructor `(string codigo, string nombre)`. Agregar acepta producto u otro kit y cantidad. Detecta ciclos directos e indirectos buscando el Id del kit receptor en el candidato, con HashSet de visitados. CalcularPrecio suma recursivamente precio por cantidad y rechaza un kit vacio. Se permite compartir un subkit entre ramas sin confundirlo con un ciclo.

En Tests estan ClienteTests, ProductoSimpleTests y KitCompuestoTests bajo `Domain/Clientes` y `Domain/Catalogo`.

### Pendiente inmediato

Continuar con Iterator y Visitor, integrar expansion a productos fisicos por SKU, propagacion de cantidades, agregacion de SKU repetidos y calculo del peso. Comprobar KIT_TELETRABAJO = P01/P02/P03, 3 unidades fisicas y 1.00 kg. La base Composite esta iniciada: P18 aun necesita demostrar expansion completa, ademas de precios y ciclos.

Todavia NO se han instalado/configurado EF Core, SQLite, DI ni migraciones; no hay autenticacion, pagos, pedidos ni pantallas funcionales de negocio. Revisar el codigo real por si el usuario hizo cambios posteriores.

## 4. Reglas de arquitectura y tecnologia

- C# .NET 8, Windows Forms en Windows, Visual Studio compatible.
- Cinco proyectos independientes con las direcciones de referencias indicadas. Domain y Application sin EF, SQLite ni WinForms.
- SQLite con EF Core 8, entidades efectivamente mapeadas, migraciones, PK/FK, indices, restricciones de unicidad, transacciones y control de concurrencia. Las listas en memoria no son la persistencia principal.
- Microsoft.Extensions.DependencyInjection en el composition root de WinForms; dependencias por constructor.
- Interfaces de Repository en el nucleo y adaptadores en Infrastructure. Unit of Work real mediante DbContext y transaccion. CQRS basico en operaciones criticas.
- Entidades y agregados protegen invariantes; sin setters publicos que permitan saltarlas.
- Usar los 23 GoF con colaboradores invocados realmente. No sustituir patrones por nombres de clases, metodos vacios, salidas fijas ni un gran switch en lugar de State.
- Aplicar SRP/OCP/LSP/ISP/DIP, KISS/DRY/YAGNI y Clean Code. La obligacion de los patrones y de la persistencia no se elimina invocando YAGNI.
- Solo cobros y notificaciones locales de demostracion; no bancos, correos ni SMS reales. Sin infraestructura de nube, REST o contenedores adicionales. Comprobantes internos, sin calculos de IVA.

## 5. Dominio, datos y reglas numericas

### Actores y acceso

- ADMIN: todas las operaciones.
- VENTAS: clientes, pedidos, validacion y cobro.
- BODEGA: preparacion, despacho y entrega.
- CONSULTA: solo lectura.

Autorizacion en Application y servicios protegidos, comprobable sin UI. Ocultar un boton no basta. Contraseñas con hash y almacenamiento seguro de salt/parametros si corresponde; cuentas de ejemplo ficticias, cierre de sesion. Para operaciones no asignadas expresamente a un rol especifico, definir una politica coherente y explicarla, respetando ADMIN y CONSULTA.

### Catalogo inicial obligatorio

| SKU | Nombre | Precio USD | Stock | Peso kg |
| --- | --- | --- | --- | --- |
| P01 | Mouse USB | 20.00 | 8 | 0.20 |
| P02 | Teclado | 30.00 | 5 | 0.70 |
| P03 | Memoria USB | 10.00 | 20 | 0.10 |
| P04 | Webcam | 40.00 | 4 | 0.35 |
| P05 | Alfombrilla | 5.00 | 30 | 0.10 |

KIT_OFICINA = P01 + P02 = 50.00.

KIT_TELETRABAJO = KIT_OFICINA + P03 = 60.00, 3 unidades fisicas, 1.00 kg.

CRUD y busqueda de productos; SKU unico. Kits anidados con cantidades; impedir ciclos. Expandir cantidades hasta los productos finales, y sumar SKU repetidos antes de comprobar existencias y generar movimientos. Comprobar desbordamientos en cantidades y stock.

El pedido conserva instantaneas de precios y composicion al registrarse. Cambiar el catalogo no altera un pedido pagado. Los clones de borradores y mementos copian profundamente todas las colecciones mutables y la estructura anidada correspondiente.

### Promociones excluyentes

1. Mayorista15 si el total de unidades fisicas finales es al menos 10, cualquiera sea la categoria del cliente.
2. Si no cumple la anterior, Vip10 si la categoria es VIP.
3. En otro caso, SinDescuento.

No sumar promociones. Una etiqueta MAYORISTA por si sola no da el 15%. Contar unidades fisicas finales de kits, no solamente renglones ni kits vendidos.

Entrega ESTANDAR: 5.00 y 3 dias. EXPRES: 12.00 y 1 dia.

```text
subtotal = suma(precio de cada componente * cantidad)
descuento = Round(subtotal * tasa, 2, MidpointRounding.AwayFromZero)
total = subtotal - descuento + tarifa
```

Usar decimal y mostrar dos decimales. Sin IVA.

### Estados y confirmacion

BORRADOR -> VALIDADO -> PAGADO -> DESPACHADO -> ENTREGADO.

CANCELADO solamente desde BORRADOR o VALIDADO. Rechazar edicion de pagados, despacho sin pago, acciones repetidas, cobros duplicados y retrocesos. Historial de transiciones trazable y consistente.

El stock se descuenta una sola vez cuando el cobro es aceptado y se confirma correctamente la operacion. Validacion o falta de stock deben detener el flujo antes del proveedor. Rechazo del proveedor no deja pago aprobado, descuentos de inventario, transicion exitosa ni Outbox de exito.

Pedido, pago, movimientos, existencias y Outbox se guardan atomicamente en la transaccion local. Idempotencia con clave y restriccion unica: repetir la misma solicitud devuelve el resultado original aunque el pedido ya este PAGADO; no volver a cobrar ni descontar. No basta verificar el estado antes de consultar la idempotencia.

Concurrencia: P36 exige MAXIMO UN COBRO APROBADO para dos confirmaciones que compiten por una existencia. No basta detectar stock negativo despues de que ambos proveedores aceptaron. Coordinar disponibilidad y concurrencia antes del cobro final, con un control equivalente correcto para SQLite, y probar con unidades de trabajo/conexiones independientes.

No se exigen transacciones distribuidas ni pagos reales. Diseñar y probar los fallos de colaboradores sin persistencia parcial indebida.

### Outbox y efectos posteriores

Insertar eventos en la MISMA transaccion que el cambio de negocio. Procesarlos despues del commit. PedidoPagado, DespachoRegistrado y DevolucionAceptada alimentan notificacion, auditoria y metricas.

Un fallo del notificador deja trabajo recuperable tras reiniciar. Evitar efectos duplicados por evento/suscriptor: si fallo un suscriptor, no repetir metricas o auditorias ya aplicadas. No recrear cobros al reintentar Outbox. Mantener evidencia de pendientes, reintento y procesamiento.

El Decorator puede registrar intentos autorizados fallidos. Una denegacion del Proxy no alcanza al proveedor ni al log de cobro; una auditoria separada de seguridad si es admisible.

### Devolucion TOTAL

Solo de ENTREGADO y no devuelto antes.

- DEFECTO: hasta 30 dias desde la entrega.
- ARREPENTIMIENTO: hasta 7 dias, producto sin uso.
- Reembolso: importe original de productos despues del descuento, sin tarifa de envio.
- Reintegro de stock cuando proceda, una sola vez. Para el caso de Ana por defecto, P35 exige reintegrar una vez.
- Devolucion rechazada sin stock/reembolso. Restriccion de una devolucion por pedido y trazabilidad del reembolso unico.

La guia no incluye un estado DEVUELTO en la lista de estados del pedido; se puede representar la devolucion mediante su relacion/indicador correspondiente y conservar la coherencia del modelo.

### Casos oficiales independientes

| Caso | Calculo | Resultado y existencias |
| --- | --- | --- |
| Ana VIP, teletrabajo x1, estandar | 60 - 6 + 5 | 59.00; P01=7, P02=4, P03=19; reembolso posterior 54.00 |
| Luis NORMAL, P04 x2 + P05 x2, expres | 80 + 10 + 12 | 102.00; P04=2, P05=28 |
| Carmen, P03 x10, estandar | 100 - 15 + 5 | 90.00; descuento 15.00; P03=10 |
| Teletrabajo x6, stock inicial | necesita 6 P02, disponibles 5 | rechazo total, sin pago ni alteracion de stock |

Restablecer una base de PRUEBAS controlada entre escenarios oficiales independientes, salvo pruebas explicitas de acumulacion, concurrencia o persistencia. No restablecer silenciosamente la base normal al iniciar la aplicacion: el seed es idempotente, no un reseteo de ventas.

## 6. Modelo persistente minimo

Usuario con rol/hash; Cliente; Producto; Kit y KitComponente; Pedido y PedidoDetalle; PedidoDetalleComponente con instantanea fisica; Pago; MovimientoInventario; Despacho; Devolucion; EventoOutbox; Auditoria. Se admiten nombres equivalentes con las mismas garantias.

PK y FK reales; indices de busqueda; unicidad de SKU, referencia de cobro, clave de idempotencia y devolucion por pedido. Seed idempotente de cinco productos, dos kits, roles y cuentas ficticias. Movimientos positivos/negativos con motivo y referencia. Usar concurrencia optimista o control equivalente contra sobreventa.

Separar catalogo actual de instantaneas de venta. Si se separan modelos EF de entidades del dominio, hacer mapeos completos y repositorios reales; no omitir persistencia de kits, snapshots, estados o historial por dificultad de mapeo.

Pruebas de integracion con SQLite real y bases aisladas. El proveedor EF InMemory no demuestra transacciones, restricciones ni concurrencia de SQLite.

## 7. Los 23 GoF requeridos y su colaboracion

### Creacionales

1. **Factory Method (P11):** creadores especializados EMAIL/SMS sobrescriben el metodo de fabrica y retornan notificadores con el mismo contrato. Seleccion fuera del formulario y avisos provenientes de eventos de pago, despacho y devolucion. Una simple fabrica con switch no sustituye la herencia/colaboracion requerida.
2. **Abstract Factory (P12):** familias ESTANDAR/EXPRES crean tarifa, plazo y etiqueta/guia compatibles; impedir mezcla inadvertida de familias. Probar 5/12 y 3/1 dias.
3. **Builder (P13):** PedidoBuilder ensambla cliente, lineas simples/kits, entrega y snapshots; agregado valido. Rechazar falta de cliente o lineas. Los datos incompletos de un formulario pueden ser una solicitud/borrador separado, sin desproteger la entidad final.
4. **Prototype (P14):** clonar profundamente un pedido BORRADOR con kit anidado, identificador nuevo y sin pago copiado. Modificar una copia no altera plantilla ni otra copia.
5. **Singleton (P15):** MetricasSesion compartido mediante DI por varias pantallas, una instancia y contadores thread-safe. La DB es la fuente de verdad del negocio; no inflar contadores por reintentos o reinicios de Outbox.

### Estructurales

6. **Adapter (P16):** PasarelaLegadaSimulada con `CobrarCentavos(long, string)` intacta, adaptada a `IPasarelaPago.CobrarAsync(decimal, clave)`. Segundo proveedor intercambiable por DI, con entradas, resultados y excepciones contractuales compatibles.
7. **Bridge (P17):** ComprobanteVenta/ComprobanteDevolucion independientes de ISalidaDocumento: VistaPrevia y ArchivoTXT. Cuatro combinaciones reales, TXT generado y vista visible.
8. **Composite (P18):** IElementoCatalogo, ProductoSimple y KitCompuesto; precios recursivos, expansion fisica, cantidades y ciclos. Base ya construida; terminar expansion e integracion de negocio.
9. **Decorator (P19):** AuditoriaPagoDecorator envuelve IPasarelaPago; inicio -> proveedor -> fin/error con correlacion, resultado preservado. Sin log de pago si Proxy deniega.
10. **Facade (P20):** ConfirmacionPedidoFacade coordina permisos, validaciones, valoracion, cobro, persistencia, stock y Outbox. Delega reglas; no contiene SQL ni implementa descuentos propios.
11. **Flyweight (P21):** CatalogoVisualFlyweightFactory comparte metadatos inmutables de SKU: nombre, categoria, descripcion. Al menos 1000 filas de vista. Stock, cantidades y precio de venta son extrinsecos. Demostrar ReferenceEquals, cantidad de objetos y comparacion equivalente de memoria; no inventar mejoras porcentuales.
12. **Proxy (P22):** PagoAutorizadoProxy permite ADMIN/VENTAS antes del Decorator/Adapter. CONSULTA/BODEGA no alcanzan proveedor ni log de cobro. Probarlo tambien sin UI.

### Comportamentales

13. **Chain of Responsibility (P23):** cliente activo -> lineas/cantidades -> stock -> autorizacion/condiciones. Rechazo estructurado y parada en primer error. No sustituye invariantes del dominio.
14. **Command (P24):** ConfirmarPedidoCommand, CancelarPedidoCommand, DespacharPedidoCommand, ProcesarDevolucionCommand; invocador registra comando, entrada y resultado. UI y pruebas ejecutan el mismo camino.
15. **Interpreter (P25/P26):** parser y AST para ESTADO = valor, TOTAL >= numero, CLIENTE = texto, AND/OR y parentesis, precedencia definida y errores legibles. Sin SQL concatenado ni reemplazar el parser por filtros fijos.
16. **Iterator (P27):** iterador propio de hojas fisicas de kits anidados, determinista, cantidades propagadas, fin correcto y sin entregar la lista interna mutable. No limitarse a renombrar foreach sin un iterador real.
17. **Mediator (P28):** FormularioPrincipalMediator coordina catalogo, carrito, pedidos y metricas tras operaciones. Sin llamadas directas entre formularios.
18. **Memento (P29):** historial de hasta cinco estados completos de PedidoBorrador, colecciones anidadas, undo/redo. Probar tres cambios, dos undo, un redo. Confirmar limpia/bloquea el historial; no deshacer cobro/stock pagado.
19. **Observer (P30):** publicador y observadores de notificacion, auditoria y metricas, mediante Outbox durable despues del commit. Efectos unicos por evento/suscriptor.
20. **State (P31):** objetos para los seis estados BORRADOR, VALIDADO, PAGADO, DESPACHADO, ENTREGADO, CANCELADO, con reglas de acciones validas/invalidas/repetidas. Sin gran switch como sustituto.
21. **Strategy (P32):** SinDescuento, Vip10 y Mayorista15, seleccion por condiciones/prioridad explicita. Extension con otra politica sin editar una cadena central de condiciones ni la calculadora.
22. **Template Method (P33):** ProcesarDevolucion fija secuencia elegibilidad -> inspeccion -> calculo reembolso -> registro devolucion/stock -> evento. Defecto y Arrepentimiento personalizan pasos permitidos. Defecto dia 20 aceptado; arrepentimiento dia 10 rechazado.
23. **Visitor (P34):** visitantes distintos sobre ProductoSimple/KitCompuesto para desglose fisico por SKU y peso logistico, con visita anidada real. Teletrabajo detalla P01/P02/P03 y pesa 1.00 kg. Evitar duplicar reglas de expansion entre visitantes/iteradores.

## 8. Interfaz minima y uso real

Doce pantallas o pestañas desde una ventana principal:

1. Acceso.
2. Panel general.
3. Catalogo y kits.
4. Clientes.
5. Carrito/borrador.
6. Pedidos e historial.
7. Cobro.
8. Despacho y entrega.
9. Devoluciones.
10. Inventario y movimientos.
11. Reportes y comprobantes.
12. Auditoria, metricas y demostraciones.

Consultar los 12 wireframes del PDF. Su distribucion puede adaptarse, conservando las operaciones. DataGridView con columnas claras, filtros/seleccion, importes legibles, validacion por campo, foco/TAB, escalado y confirmacion de operaciones irreversibles.

Demostraciones reales: cambiar proveedor, inducir fallo de cobro y fallo del notificador, reprocesar Outbox, recorrer patrones, observar logs. No mostrar resultados fijos ni cargar contadores ilustrativos de los wireframes como ventas reales.

Detalles detectados al analizar los wireframes: si una imagen rotula P03 como Cable HDMI, manda la tabla oficial, que es Memoria USB. El reembolso oficial de Ana es 54.00, no 59.00. Los conteos de dashboard del dibujo son ilustrativos.

## 9. Matriz oficial P01-P38, toda pendiente de comprobacion integral

Para cada caso conservar ID, entrada/precondicion concreta, esperado, obtenido real, evidencia, fecha, archivo/metodo. No marcar APROBADO por haber escrito una prueba o por inferencia. Las pruebas de rechazo comprueban ausencia de efectos indebidos. Ejecutar las 38 y registrar sus resultados.

| ID | Escenario y criterio |
| --- | --- |
| P01 | DB inexistente, migraciones y seed; 5 productos, 2 kits, roles; seed repetido sin duplicados. |
| P02 | Ana VIP, teletrabajo x1 estandar; 59.00, P01=7 P02=4 P03=19, PAGADO y un pago. |
| P03 | Luis NORMAL, P04x2 P05x2 expres; 102.00, P04=2 P05=28, un pago. |
| P04 | Carmen, P03x10 estandar; 90.00, descuento15.00, P03=10. |
| P05 | Teletrabajox6 con stock inicial; falta P02, sin pago, sin stock alterado ni evento de exito. |
| P06 | Error de cobro en pedido valido; inventario y estado previo intactos, sin pago aprobado ni Outbox de exito. |
| P07 | Repetir misma clave; resultado original, un pago y un movimiento por SKU. |
| P08 | PAGADO: cancelar/editar precio denegados, datos e historial intactos. |
| P09 | BODEGA despacha/entrega en secuencia permitida; despacho sin pago rechazado sin cambios. |
| P10 | Cerrar/reabrir: pedido, stock, pago y auditoria persisten en SQLite. |
| P11 | EMAIL/SMS mediante Factory Method, creador y notificador correctos tras evento. |
| P12 | Familias estandar/expres, tarifas5/12 plazos3/1 guias coherentes. |
| P13 | Builder incompleto rechazado, completo produce agregado valido. |
| P14 | Prototype de borrador con teletrabajo: cambio de cantidad no altera plantilla/copia anterior. |
| P15 | MetricasSesion misma instancia en dos vistas, contadores observables thread-safe. |
| P16 | Adapter y proveedor alternativo cobran con contrato compatible, legado intacto. |
| P17 | Dos comprobantes por dos salidas: cuatro combinaciones, vista visible y TXT real. |
| P18 | Composite: 50/60, expansion correcta de kits y ciclo rechazado. |
| P19 | Decorator: log inicio/proveedor/fin o error, correlacion y resultado preservado. |
| P20 | Facade con fallo de colaborador; coherencia sin persistencia parcial indebida. |
| P21 | 1000 filas Flyweight; identidad compartida, numero de instancias y memoria equivalente documentados. |
| P22 | CONSULTA/BODEGA denegados por Proxy antes de proveedor y log de cobro. |
| P23 | Chain informa primer error y, al corregirlo, el siguiente, sin cobro prematuro. |
| P24 | Confirmar desde UI e invocador de prueba usa el mismo comando, resultado y registro. |
| P25 | Interpreter: ESTADO = PAGADO AND TOTAL >= 50 filtra correctamente. |
| P26 | Interpreter: sintaxis erronea y parentesis; error legible y precedencia correcta sin SQL concatenado. |
| P27 | Iterator: teletrabajo recorre P01/P02/P03 con cantidades correctas, sin bucle. |
| P28 | Mediator refresca catalogo, pedidos y metricas sin acoplar formularios entre si. |
| P29 | Memento: tres cambios, dos undo y un redo, restauracion completa sin deshacer pagos. |
| P30 | Observer: PedidoPagado notifica, audita y cuenta una vez mediante tres suscriptores. |
| P31 | State: ciclo valido y rechazo de PAGADO a CANCELADO, sin escritura directa. |
| P32 | Strategy VIP/MAYORISTA/NORMAL da 6/15/0 y permite extender una politica sin cambiar calculadora central. |
| P33 | Template: defecto dia20 aceptado; arrepentimiento dia10 rechazado sin stock/reembolso. |
| P34 | Visitor: desglose P01/P02/P03 y peso1.00kg para teletrabajo. |
| P35 | Ana ENTREGADO, defecto y repeticion: reembolso54 sin flete, un reintegro y devolucion unica. |
| P36 | Dos confirmaciones concurrentes, SKU con stock1: maximo un cobro aprobado y stock no negativo. |
| P37 | Falla notificador y se reinicia procesador; recupera pendiente sin duplicar efectos ni cobro. |
| P38 | LSP/ISP/DIP, reemplazo de proveedor, contratos pequeños, referencias correctas, build limpio y minimo25 automaticas aprobadas. |

## 10. Calidad, trazabilidad y evidencias durante la construccion

- Al menos 12 requisitos funcionales y 8 no funcionales medibles, trazados a casos de uso/pruebas.
- Diagrama de contexto, componentes, clases criticas, datos/relaciones, estados y dos secuencias: confirmacion aprobada y operacion rechazada.
- Minimo 25 pruebas automaticas distribuidas entre dominio, Application e integracion. Las 52 actuales son solo dominio; completar los otros niveles y los escenarios reales, no considerar terminada la validacion por superar el numero.
- Los casos automaticos pueden cubrir aceptaciones; no se exige inventar 25+38 pruebas disjuntas. Si una aceptacion requiere UI, persistencia/reinicio o evidencia operacional, cubrirla tambien en ese nivel.
- Registrar al menos 10 riesgos REALES de diseño y refactorizaciones efectivas con Git y prueba antes/despues. Registrar decisiones a medida que aparezcan para no perderlas. No introducir defectos ni falsificar capturas o ejecuciones.
- Matriz de 23 patrones: problema, clases, archivo/metodo, recorrido, prueba, evidencia, beneficio y costo.
- Evidencia especifica para P19/P22/P37 y condiciones reales de medicion de Flyweight.
- Guardar resultados reales en logs/TRX y bitacora de aceptacion; conservar un registro reproducible de datos iniciales, comandos y resultados. Proteger datos y no versionar la base personal de ejecucion.
- Validar estados repetidos, limites de fechas de devolucion, prioridad de promociones, redondeo AwayFromZero, cantidades duplicadas/anidadas, idempotencia tras PAGADO, falta de stock, rollback, concurrencia, reinicio y reintentos de suscriptores.
- La visualizacion debe usar los mismos casos de uso que las pruebas. Sin SQL, tarifas, descuentos o logica de estados en formularios.

## 11. Trabajo autonomo sugerido

1. Leer estos archivos y el PDF completo, inspeccionar AGENTS.md y el codigo real. Verificar ruta, git status/log, referencias, SDKs, build y suite existente. No asumir acceso solo por una captura.
2. Preparar un plan coherente para todo el programa y mantener un punto de estado/pendientes en el repositorio. Conservar los cuatro commits iniciales y cualquier cambio posterior del usuario.
3. Completar Iterator/Visitor y el dominio restante: promociones, entregas, borradores/pedidos validos, snapshots, estados, clonacion y mementos.
4. Incorporar persistencia y seed, repositorios/UoW, identidad/roles, casos de uso, pagos, confirmacion consistente, Outbox, despacho/entrega y devoluciones. Se puede variar el orden tecnico para sostener un desarrollo integrado.
5. Integrar los demas patrones en los caminos reales. Mantener observabilidad y pruebas de fallos desde que se implementan los flujos.
6. Construir las 12 vistas WinForms, composicion DI, mediacion y demostraciones. Verificar en Windows de verdad: ejecutar la app, recorrer ventas/devoluciones, cerrar/reabrir y observar persistencia.
7. Ejecutar toda la matriz y las pruebas automaticas; corregir, volver a comprobar lo afectado y hacer commits reales. No detenerse despues de un esqueleto o de simples clases de patrones sin flujo completo.
8. Entregar programa completo, README, evidencia reproducible y resumen de commits/pruebas. Preparar un ZIP limpio para que William lo revise aqui. Explicar cualquier bloqueo o requisito no satisfecho: no presentarlo como completado.

Usar paquetes oficiales compatibles con EF Core 8 y fijar versiones reproducibles. Revisar documentacion primaria cuando se necesite confirmar APIs. No agregar servicios o tecnologias ajenos a la guia para facilitar una demostracion parcial.

## 12. Criterio de terminacion de esta etapa

Solucion completa con cinco proyectos, build reproducible, DB SQLite creada desde cero por migraciones/seed idempotente, autenticacion y permisos, catalogo/clientes/pedidos/pagos/despachos/devoluciones, transacciones/idempotencia/concurrencia/Outbox, 23 patrones efectivamente integrados, 12 vistas funcionales, pruebas automaticas y 38 aceptaciones ejecutadas con evidencia real.

README: prerrequisitos, restauracion, compilacion, migraciones, seed, ejecucion, cuentas ficticias, pruebas y reset seguro de datos de prueba. Mantener .git del repositorio y su historial. El ZIP de entrega excluye bin, obj, .vs, secretos y la DB personal de ejecucion.

La guia pide finalmente DOS archivos: `APE04_LogiMarket360_APELLIDOS_NOMBRES.zip` y `APE04_Informe_APELLIDOS_NOMBRES.pdf`. El usuario pospuso el informe formal y GitHub hasta terminar el programa. Preservar ahora la evidencia que se necesitara para ese informe; no afirmar que ese PDF esta terminado.

El informe posterior tiene 16 bloques obligatorios (consultar PDF): portada, resumen ejecutivo, problema/requisitos, dominio/datos, arquitectura, Clean Code/SOLID, creacionales, estructurales, comportamentales, desarrollo visual/flujo, pruebas, riesgos/limites, conclusiones por objetivos, recomendaciones por conclusiones, referencias IEEE y anexos. Respetar tambien los apartados institucionales 2.7 a 2.12. Se recuerda para conservar insumos, no para adelantar la redaccion formal.
