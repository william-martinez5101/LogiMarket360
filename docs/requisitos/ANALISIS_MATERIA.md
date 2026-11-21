# Análisis integral de las fuentes de Patrones de Software

Universidad Técnica de Ambato · Ingeniería de Software  
Fecha de revisión: 18 de noviembre de 2026

Este documento reúne el análisis del material disponible en las fuentes del proyecto. Su propósito es establecer una base compartida para desarrollar posteriormente el proyecto de la materia. Las observaciones técnicas distinguen lo explicado por las fuentes de las precisiones que conviene aplicar al implementar una solución completa.

## 1. Alcance y resultado de la revisión

Se revisaron los 19 archivos disponibles: 10 presentaciones con 310 diapositivas, 7 PDF con 168 páginas, un ZIP con 11 proyectos de consola C# y un póster. La revisión incluyó texto, notas de las presentaciones, tablas, diagramas, ejemplos de código incorporados como imágenes y los archivos fuente, configuración, README y salidas esperadas de los ejercicios del ZIP.

El ZIP contiene también archivos generados de compilación, principalmente en `bin` y `obj`. Estos se identificaron como productos del entorno, separados del código de los 11 ejemplos. La revisión de los proyectos fue estática: las salidas incluidas en el paquete son resultados esperados de referencia, y este análisis no constituye una certificación de compilación o ejecución.

El eje de la materia es aprender a reconocer problemas de diseño y resolverlos mediante responsabilidades bien ubicadas, contratos coherentes y patrones justificados. La progresión del material es: fundamentos del diseño, diagnóstico de problemas, Clean Code, SOLID, arquitectura y dominio, patrones creacionales, estructurales y comportamentales, y comprobación del resultado mediante pruebas y evidencia.

## 2. Análisis de cada archivo

Los identificadores F01–F19 permiten localizar las fuentes que sustentan los apartados posteriores.

| Ref. | Archivo original | Extensión revisada | Contenido y aporte |
|---|---|---|---|
| F01 | [1] Primera_clase_Patrones_de_Software_UTA.pptx | 16 diapositivas | Introduce diseño de software, patrón, principio, buena práctica y antipatrón. Explica deuda técnica, refactorización, calidad y seguridad mediante matrícula, pedidos y creación de reportes. Presenta las cuatro unidades y pide diagnosticar problemas antes de seleccionar patrones. |
| F02 | [2]Detección de antipatrones.pptx | 8 diapositivas | Desarrolla el diagnóstico de un `VentaService` que calcula, verifica inventario, cobra, factura y notifica. Incluye nombres expresivos, simplificación de condiciones y separación de responsabilidades. El ejercicio exige explicar el impacto del diseño y proponer una refactorización. |
| F03 | [3]RESUMEN INTRODUCCION A PATRONES DE SOFTWARE.pdf | 6 páginas | Consolida diseño, reutilización de conocimiento, acoplamiento, responsabilidades, antipatrones, deuda técnica y refactorización. Introduce seguridad mediante el problema de las credenciales incrustadas y relaciona calidad con mantenibilidad, modularidad y extensibilidad. |
| F04 | [4]Clase_3_Clean_Code_Patrones_Software.pptx | 48 diapositivas | Explica KISS, DRY, YAGNI, nombres, convenciones de Java/JavaScript/C#, funciones pequeñas, guard clauses, cohesión, acoplamiento y errores explícitos. Incluye modularidad, Jest, JUnit, xUnit, AAA, Git, documentación y ESLint. Propone una práctica con diagnóstico, refactorización y comprobación. |
| F05 | [5]Manual_Practica_Clean_Code_Node_Git_Testing (1).pdf | 14 páginas | Lleva el registro de pedidos de TecnoMarket a módulos Node.js: calculadora, servicio coordinador y punto de entrada. Incluye cinco pruebas Jest, README, ESLint, historial progresivo, evidencias y rúbrica. El caso calcula subtotal 70, descuento de 10 % y envío de 5 para obtener 68. Añade adaptaciones breves a Java y C#. |
| F06 | [6]Presentacion_PedidoManager_SOLID_Codigo_Completo.pptx | 26 diapositivas | Muestra la transformación de `PedidoManager` en modelos, políticas de descuento, calculadora, puertos, infraestructura, servicio y consola. Desarrolla especialmente SRP, OCP, ISP, DIP y DI, con composición manual en `Program.cs`. LSP aparece en la tabla conceptual de SOLID. |
| F07 | [7]PATRONES C#.pdf | 32 páginas | Guía extensa de SOLID, refactorización, Capas, Clean, Hexagonal, Onion y Vertical Slice. Desarrolla DDD, entidades, objetos de valor, agregados, invariantes y repositorios. Sigue un flujo ASP.NET Core con Request, Command, UseCase, dominio, EF Core y Unit of Work, y separa pruebas unitarias, integración y end-to-end. |
| F08 | [8]PATRONES JS.pdf | 35 páginas | Traslada el mismo razonamiento a Node.js/CommonJS y Express. Explica contratos de comportamiento en JavaScript, composición, DI explícita, campos privados, objetos de valor e inmutabilidad. Incluye MySQL, Nodemailer, políticas de descuento, Jest, fakes, separación `app.js`/`server.js`, actividades y preguntas de repaso. |
| F09 | [9]resumen1.pptx | 33 diapositivas | Sintetiza SOLID, los cinco estilos arquitectónicos, DDD y el flujo de pedidos. Es útil para relacionar síntomas con soluciones y distinguir responsabilidades de API, Application, Domain e Infrastructure. Incluye mapas de DTO/Command/entidad y pruebas por nivel. |
| F10 | [10]creacionales.pptx | 26 diapositivas | Explica los cinco creacionales mediante reportes, libros, configuración, familias de interfaz y plantillas. Compara intención, señales de uso y costos. Conecta los patrones con Repository, Unit of Work, CQRS y Application, aclarando que estos últimos no son patrones creacionales GoF. |
| F11 | [11]Awinforms_clean_architectu (1).pptx | 60 diapositivas | Integra TECHSTORE con cuatro proyectos: Domain, Application, Infrastructure y WinForms. Incluye entidad, Builder, DTO, resultado de operación, fábrica de notificaciones, casos de uso, repositorio en memoria, UoW y DI. Explica controles, eventos, referencias, NuGet, conversiones, DataGridView, CQRS y evidencias de funcionamiento. |
| F12 | [12]refuerzo_patrones_creacionales_clean_architecture.pptx | 28 diapositivas | Refuerza la ubicación de patrones y responsabilidades mediante MiniBiblioteca. Relaciona Builder para préstamos, Factory para avisos, Prototype para plantillas, repositorio, UoW y CQRS básico. Destaca la separación de formato visual y reglas, los ciclos de vida de DI y la pérdida de datos al cerrar una aplicación que usa memoria. |
| F13 | [13]ParalaEvaluacion.pdf | 16 páginas | Reúne los conceptos de la Unidad 2 para reconocerlos en código. Incluye SOLID, los cinco creacionales, Repository, UoW, CQRS, Clean Architecture, DI, WinForms y copia profunda. Sus comparaciones ayudan a defender las decisiones del proyecto y evitar patrones meramente nominales. |
| F14 | [14]patrones_estructurales.pptx | 32 diapositivas | Desarrolla los siete estructurales: intención, participantes, diagramas, código antes/después, ejemplos, errores y criterios de selección. Incluye adaptación de pagos, fachada de ventas, logging de repositorio, proxy de reportes, catálogo compuesto, tipos/canales de notificación y tipos de vehículos compartidos. |
| F15 | [15]Patrones_Caso_Integrador.pptx | 33 diapositivas | Integra Composite, Adapter, Decorator, Proxy y Facade en compras de combos. Establece roles, total 7.50, auditoría y proveedor heredado. Incluye cuatro retos: galleta con total 8.25, ADMIN, inversión de envoltorios y proveedor V2. Justifica excluir Bridge y Flyweight en ese caso. |
| F16 | [16]Patrones_Comportamentales.pdf | 54 páginas | Explica los 11 comportamentales con propósito, participantes, recorrido, aplicaciones, ventajas y límites. Incluye correspondencias en Java y Python, referencias a TypeScript y la colaboración con patrones creacionales y estructurales. |
| F17 | Ejercicios_Refactorizacion.pdf | 11 páginas | Presenta un ejercicio por patrón comportamental con problema, código base, cinco condiciones de implementación y pruebas de aceptación. Define comportamientos concretos, incluidos errores y casos límite, para comprobar una refactorización. |
| F18 | patrones_comportamentales(ejercicios_refactorizacion_completos).zip | 11 proyectos C# | Cada ejemplo contiene `Program.cs`, `.csproj`, README y salida esperada. Todos los proyectos declaran `net8.0`, C# 12, nullable e implicit usings. El paquete permite identificar directamente los participantes de los patrones y contrastarlos con F16 y F17. |
| F19 | poster.png | Póster completo | Resume visualmente los siete estructurales mediante diagramas, propósito, antes/después y tabla comparativa. Sirve como apoyo para estudiar diferencias y reconocer la intención de cada patrón. Sus fragmentos de código requieren contexto adicional para convertirse en programas completos. |

## 3. Fundamentos que atraviesan toda la materia

### Diseño y propiedades de calidad

Las fuentes relacionan el diseño con la organización de componentes y sus dependencias. El criterio de calidad combina comportamiento correcto con comprensibilidad, mantenibilidad, reutilización, extensibilidad, modularidad y facilidad de prueba. También aparecen observabilidad, confiabilidad, trazabilidad, colaboración y seguridad.

Un patrón representa conocimiento reutilizable de diseño. Debe estudiarse con su contexto, problema, estructura, participantes y consecuencias. La implementación se adapta al lenguaje y al dominio; memorizar nombres o reproducir diagramas no demuestra que la solución resuelva una necesidad.

Conviene mantener estas distinciones:

| Concepto | Significado en el material | Ejemplo |
|---|---|---|
| Principio | Criterio para evaluar decisiones de diseño. | SRP, OCP, KISS. |
| Buena práctica | Acción concreta que facilita el trabajo. | Nombrar métodos por su intención y registrar commits claros. |
| Patrón de diseño | Organización recurrente para resolver un problema bajo ciertas condiciones. | Strategy para políticas intercambiables. |
| Estilo arquitectónico | Organización del sistema y sus fronteras de dependencia. | Clean, Hexagonal, Vertical Slice. |
| Code smell | Síntoma que invita a investigar el diseño. | Método largo, nombre ambiguo, duplicación. |
| Antipatrón | Solución recurrente con consecuencias perjudiciales. | Clase Dios, lógica concentrada y difícil de modificar. |
| Deuda técnica | Costo futuro asociado a decisiones incompletas o compromisos de diseño. | Reglas duplicadas o dependencias difíciles de reemplazar. |
| Refactorización | Cambio de estructura que conserva el comportamiento acordado. | Extraer el cálculo de un coordinador de pedidos. |

La cohesión describe cuánto se relacionan las responsabilidades internas de un componente. El acoplamiento describe sus dependencias con otros. La meta práctica es que cada componente tenga un propósito coherente y dependa de contratos que necesite realmente.

### Clean Code

F04 y F05 desarrollan tres principios centrales. KISS orienta a una solución sencilla que cumpla el requisito. DRY centraliza conocimiento y reglas para evitar fuentes de verdad inconsistentes. YAGNI evita construir funciones o abstracciones basadas únicamente en posibilidades futuras.

Las técnicas concretas incluyen nombres expresivos, constantes con significado, funciones de intención clara, reducción de anidamiento, salidas tempranas, módulos cohesionados, dependencias explícitas y errores que permitan entender qué condición falló. Las convenciones se adaptan al lenguaje: los métodos C# utilizan PascalCase, mientras que los ejemplos Java y JavaScript utilizan camelCase.

La separación entre cálculo y presentación es recurrente. Una operación de cálculo debería poder verificarse sin imprimir por consola, abrir una ventana o enviar correo. El servicio de aplicación coordina colaboradores y devuelve datos o un resultado comprensible para la presentación.

La seguridad aparece desde los fundamentos: evitar credenciales incrustadas y proteger operaciones sensibles. En F15 se materializa mediante autorización antes del cobro. Para el proyecto, estas ideas se traducirán en validación consistente, configuración externa de secretos y permisos aplicados en la operación protegida.

## 4. SOLID y su aplicación

| Principio | Interpretación para el proyecto | Señal que debe motivar revisión | Evidencia útil |
|---|---|---|---|
| SRP | Agrupar responsabilidades que cambian por motivos relacionados. | Un formulario o servicio calcula, persiste, notifica y decide presentación. | Poder modificar persistencia o presentación sin cambiar reglas de cálculo. |
| OCP | Estabilizar el algoritmo general y permitir extensiones en los puntos de variación justificados. | Cada promoción o proveedor amplía un mismo bloque central. | Agregar una política mediante otra implementación y su registro. |
| LSP | Mantener las expectativas del contrato al sustituir una implementación. | Un tipo promete una capacidad que sus subtipos no pueden cumplir. | Verificar el mismo contrato con varias implementaciones. |
| ISP | Definir capacidades pequeñas y cohesionadas según sus consumidores. | Implementaciones con métodos irrelevantes, vacíos o artificialmente no soportados. | Cada consumidor utiliza las operaciones del contrato que recibe. |
| DIP | Hacer que la lógica de alto nivel trabaje con abstracciones apropiadas. | Un caso de uso construye SQL, SMTP o un proveedor concreto. | Ejecutar el caso de uso con un adaptador alternativo o un fake. |

SRP no exige un solo método por clase. OCP tampoco implica que ningún archivo pueda cambiar: registrar una nueva implementación en el punto de composición es parte del diseño. La presencia de una excepción no demuestra por sí sola una violación de LSP; importa si contradice el comportamiento prometido por el contrato.

DIP y DI deben explicarse por separado. DIP es el principio que orienta las dependencias; DI es una técnica para suministrar colaboradores desde fuera. `Program.cs`, `app.js` o un punto de composición pueden conocer las implementaciones concretas y conectarlas. Usar `new` para crear una entidad u otro objeto local no constituye automáticamente un problema de diseño.

## 5. Arquitectura y modelado del dominio

### Estilos presentes en las fuentes

| Estilo | Idea principal | Aplicación que muestran las fuentes |
|---|---|---|
| Capas | Separar presentación, coordinación, dominio y detalles técnicos. | Extraer negocio y acceso a datos de un Controller o formulario. |
| Clean Architecture | Dirigir dependencias de código hacia las políticas internas. | Domain sin EF Core, SQL, Express ni controles visuales. |
| Hexagonal | Conectar el núcleo mediante puertos y adaptadores. | Entrada HTTP o consola y salidas de persistencia o notificación. |
| Onion | Organizar dependencias alrededor del dominio como centro. | Modelo estable con servicios de aplicación alrededor e infraestructura externa. |
| Vertical Slice | Agrupar código por funcionalidad o caso de uso. | Crear, consultar o cancelar con handlers específicos. |

Estos enfoques pueden compartir decisiones, pero no son nombres intercambiables. Una estructura de carpetas sirve como apoyo; el cumplimiento depende de las referencias, imports y colaboraciones reales.

### Responsabilidades de las capas

| Capa | Responsabilidades | Límites relevantes |
|---|---|---|
| Presentation | Capturar datos, comprobar formato, convertir a entradas del caso de uso y presentar resultados. | Evitar SQL, repositorios concretos y reglas centrales dentro de botones o endpoints. |
| Application | Coordinar casos de uso, comandos, consultas, puertos y resultados. | Evitar controles visuales y dependencia directa de drivers o proveedores externos. |
| Domain | Expresar conceptos, comportamiento e invariantes del negocio. | Conservar independencia de presentación y tecnología de persistencia. |
| Infrastructure | Implementar persistencia y comunicaciones concretas. | Respetar contratos internos y mantener fuera de esta capa las decisiones centrales del negocio. |

La secuencia de una operación puede alcanzar infraestructura desde un caso de uso mediante un contrato. Esa llamada en ejecución no obliga a que el código de Application o Domain referencie la implementación concreta. En el proyecto habrá que distinguir el diagrama de flujo de llamadas del diagrama de dependencias de código.

Las interfaces pueden ubicarse en Application o Domain según a quién pertenezca la necesidad. El lugar no lo determina un nombre de carpeta universal. La implementación concreta reside en el borde técnico y depende del contrato interno.

### DDD

F07, F08 y F09 desarrollan principalmente modelado táctico del dominio: lenguaje ubicuo, entidades, objetos de valor, agregados, raíz de agregado e invariantes. F08 incorpora además servicios y eventos de dominio. Los materiales no desarrollan con la misma profundidad los aspectos estratégicos de DDD, como un mapa completo de contextos y sus relaciones.

Una entidad tiene identidad y ciclo de vida. Un objeto de valor representa un concepto por sus valores, como Dinero, y debe proteger su consistencia. Un agregado delimita una unidad de consistencia; su raíz controla los cambios de sus elementos internos. Una invariante es una condición que debe mantenerse válida, como cantidad positiva o imposibilidad de confirmar un pedido vacío.

Un modelo con comportamiento concentra reglas cerca del estado que protege. DTO, Request y Command cumplen funciones de intercambio o entrada y no sustituyen al modelo del negocio. La validación de formato en la interfaz puede mejorar la experiencia, mientras que la invariante debe seguir protegida al utilizar el dominio desde cualquier entrada.

## 6. Los 23 patrones GoF

La cobertura conjunta comprende 5 creacionales, 7 estructurales y 11 comportamentales. Que todos formen parte del contenido de estudio no establece una obligación de incluirlos todos en el futuro proyecto.

### Creacionales

| Patrón | Problema e intención | Referencia práctica | Cuidado al implementar |
|---|---|---|---|
| Factory Method | Delegar la creación de un producto a un método que los creadores concretos especializan. | `ReporteFactory`, `PdfFactory`, `ExcelFactory`; fábricas de notificaciones. | Distinguir esta estructura de una fábrica simple con `switch`. |
| Abstract Factory | Crear familias de productos relacionados y compatibles. | UI Windows/Mac; entrega y empaque Normal/Premium. | Debe existir una familia de productos, no únicamente una selección de un objeto. |
| Builder | Construir un objeto complejo con pasos expresivos. | Libro, Pedido y préstamo con datos opcionales. | El resultado debe satisfacer invariantes y las construcciones sucesivas no deben compartir accidentalmente estado mutable. |
| Prototype | Crear objetos a partir de copias controladas de una plantilla. | PlantillaReporte, PlantillaPrestamo y PedidoPlantilla. | Definir qué se comparte y qué necesita copia profunda. |
| Singleton | Controlar una instancia única dentro del alcance definido por el diseño. | Configuración y Bitacora con `Lazy<T>`; ciclos de vida administrados por DI. | Evitar estado global oculto y comprobar el alcance de la garantía y la concurrencia. |

### Estructurales

| Patrón | Problema e intención | Ejemplo de las fuentes | Consecuencia a evaluar |
|---|---|---|---|
| Adapter | Traducir una interfaz existente al contrato que necesita el cliente. | `IPagoService.Pagar` hacia `PayPalApi.MakePayment`; pasarela heredada y proveedor V2. | La traducción debe respetar datos, unidades, resultado y errores del contrato. |
| Bridge | Permitir que dos dimensiones cambien de forma independiente. | Tipo de notificación y canal; tipo de comprobante y medio. | Evitar una estructura adicional cuando solo existe un eje real de variación. |
| Composite | Tratar hojas y grupos mediante una operación común. | Producto y Combo; Producto y Categoria. | Mantener consistencia de la composición y evitar ciclos si se requiere un árbol. |
| Decorator | Añadir responsabilidades mediante envoltorios del mismo contrato. | Logging o auditoría alrededor de un pago o repositorio. | Definir el orden y el comportamiento cuando la operación interna falla. |
| Facade | Ofrecer una entrada sencilla a la colaboración de varios componentes. | ConfirmarPedido y VentaFacade. | Mantener la delegación para que la fachada conserve una responsabilidad coherente. |
| Flyweight | Compartir estado intrínseco entre muchas instancias. | TipoVehiculo compartido; árboles e íconos del póster. | Separar estado compartido y propio, preferir datos compartidos inmutables y justificar el ahorro. |
| Proxy | Controlar acceso, creación o invocación de un objeto real. | Permisos de reportes y pago según rol; creación diferida. | Proteger la operación efectiva y hacer comprensible la delegación. |

Las diferencias más relevantes son Adapter frente a Bridge, Decorator frente a Proxy y Facade frente a una clase que concentra toda la lógica. Sus estructuras pueden parecerse, pero la intención determina el patrón.

### Comportamentales

| Patrón | Responsabilidad principal | Participantes o mecanismo visible | Caso de estudio |
|---|---|---|---|
| Chain of Responsibility | Resolver o delegar una solicitud por una cadena configurable. | Solicitud, Aprobador y responsables concretos. | Técnico, supervisor y gerente según costo. |
| Command | Representar una acción ejecutable como objeto. | IComando, EncenderLuz, ControlRemoto, Luz e historial. | Encender y deshacer conservando el estado previo. |
| Interpreter | Evaluar expresiones de un lenguaje de reglas acotado. | IExpresion, terminales y nodos Y/O. | VIP Y (ACTIVO O COMPRAS ≥ 5). |
| Iterator | Recorrer una colección sin exponer su almacenamiento. | IEnumerable, IEnumerator y `yield return`. | Catálogo de libros. |
| Mediator | Centralizar reglas de interacción entre colaboradores. | IMediador, FormularioReservas y controles. | Habilitar confirmación con cancha elegida y horas válidas. |
| Memento | Guardar y recuperar estado respetando su encapsulación. | Editor, EstadoGuardado e Historial. | Restaurar un editor desde “Hola mundo” a “Hola”. |
| Observer | Comunicar cambios a interesados registrados. | Pedido, IObservadorPedido y suscriptores. | Avisos a correo y auditoría. |
| State | Delegar comportamiento y transiciones en el estado vigente. | Reserva, IEstadoReserva, Pendiente, Confirmada y Cancelada. | Confirmación, cancelación y rechazo de transiciones inválidas. |
| Strategy | Intercambiar algoritmos mediante composición. | CalculadoraPedido e IDescuento. | Políticas normal, estudiante y temporada. |
| Template Method | Mantener una secuencia y especializar pasos. | ExportadorReporte, ReporteCsv y ReporteTexto. | Inicio, datos, formato y cierre de reportes. |
| Visitor | Agregar operaciones a tipos de elementos relativamente estables. | IElementoCarrito, IVisitante y doble despacho. | Envío e impuesto didáctico para productos físicos y digitales. |

Strategy se centra en algoritmos intercambiables y State en comportamiento condicionado por el ciclo de vida. Command representa una acción, mientras Memento conserva un estado; pueden colaborar en una función de deshacer. Observer distribuye un cambio a interesados y Mediator decide cómo interactúan varios colaboradores. Template Method se apoya en una secuencia compartida y herencia; Strategy sustituye colaboradores por composición. Visitor facilita nuevas operaciones, pero incorporar nuevos tipos de elemento exige revisar los visitantes.

## 7. Patrones y mecanismos complementarios

Repository, Unit of Work y CQRS aparecen junto con los creacionales por su relación con Application y persistencia. Su clasificación debe mantenerse separada de las tres familias GoF.

| Concepto | Responsabilidad | Diferencia que debe quedar clara |
|---|---|---|
| Repository | Ofrecer operaciones de acceso a agregados mediante un contrato. | Un nombre `Repository` no desacopla nada si los casos de uso siguen accediendo directamente a SQL o a una lista global. |
| Unit of Work | Coordinar la confirmación de un conjunto de cambios. | Un método vacío en memoria no aporta por sí mismo atomicidad ni rollback. |
| CQRS | Separar operaciones de modificación y lectura. | No exige MediatR ni bases de datos distintas para comprender su separación básica. |
| DI | Suministrar dependencias desde el exterior. | No equivale automáticamente a DIP ni al patrón Singleton. |
| Composition Root | Conectar contratos e implementaciones en el borde. | La selección de detalles se concentra fuera de las reglas de negocio. |

Un Command de CQRS puede ser un objeto de datos que representa una intención y que procesa un handler. Esto no implica automáticamente una implementación completa del patrón GoF Command con `Ejecutar` y `Deshacer`.

## 8. Relación entre los ejercicios y su evidencia

### Los 11 ejercicios comportamentales

| Ejercicio | Comportamiento de referencia | Comprobaciones adicionales relevantes |
|---|---|---|
| Cadena | 80 autoriza Técnico; 900 autoriza Gerente; 9000 queda fuera de política. | Límites exactos 100, 500 y 5000; costo inválido; orden de la cadena. |
| Command | Encender produce ENCENDIDA y deshacer recupera APAGADA. | Historial vacío y estado inicialmente encendido; identidad de cada ejecución del comando. |
| Interpreter | Con VIP=true, activo=false y 6 compras la expresión es verdadera. | Los otros dos escenarios de aceptación: compras insuficientes y cliente no VIP. |
| Iterator | Muestra título y disponibilidad mediante foreach. | Catálogo vacío, orden de recorrido y encapsulación de la colección. |
| Mediator | Elegir cancha con 0 horas da False; cambiar a 2 da True. | Volver a 0 debe deshabilitar el botón. |
| Memento | Muestra “Hola mundo” y restaura “Hola”. | Historial vacío y varias instantáneas independientes. |
| Observer | CONFIRMADO avisa a correo y auditoría; ENTREGADO avisa solo a auditoría después de retirar correo. | Evitar suscripción duplicada y tolerar cambios de suscripción durante el recorrido. |
| State | PENDIENTE, CONFIRMADA, CANCELADA y rechazo de confirmación posterior. | Confirmación repetida, cancelación repetida y cancelación desde pendiente. |
| Strategy | Subtotal 100 produce 100, 90 y 80 según política. | Rechazo de subtotales negativos y límites de las políticas. |
| Template Method | CSV y texto mantienen el orden de inicio, datos, formato y cierre. | Secuencia común y especialización exclusiva de los pasos permitidos. |
| Visitor | Envío total 6 e impuesto didáctico 12.5. | Independencia de visitantes y acumulación controlada al reutilizar una operación. |

Los escenarios de referencia están en F17 y F18. La columna de comprobaciones combina condiciones explícitas de aceptación con extensiones sugeridas por el análisis para una implementación más completa. No todas esas comprobaciones se ejecutan desde los `Main()` incluidos, y el paquete no contiene una suite automatizada que sustituya esa verificación.

### El caso integrador estructural

El pedido base compone café de 2.50, sánduche de 3.75 y agua de 1.25. El combo desayuno suma 6.25 y el refrigerio, que contiene ese combo, suma 7.50. Agregar la galleta de 0.75 cambia el refrigerio a 8.25 mediante composición.

El orden de colaboración autorizado es Facade, Proxy, Decorator, Adapter y proveedor. Composite calcula el importe que utiliza la operación. El Proxy externo rechaza a VISITANTE antes de alcanzar el registro de cobro y la pasarela. CAJERO y ADMIN llegan al cobro con LOG de inicio y fin.

Invertir Proxy y Decorator hace que se registre el intento denegado, aunque el cobro siga bloqueado. Esto muestra que el orden de composición tiene consecuencias observables. La ausencia de LOG en el rechazo es una regla específica del ejercicio sobre el registro de cobro; no debe convertirse automáticamente en una regla general para toda auditoría de seguridad.

El reemplazo por un proveedor V2 prueba la frontera del Adapter. La exclusión de Bridge y Flyweight demuestra que también hay que justificar los patrones que se descartan.

## 9. Pruebas, herramientas y documentación

Las fuentes distinguen pruebas unitarias de dominio, unitarias de aplicación con fakes, integración de adaptadores y end-to-end del flujo completo. También explican regresión y la estructura AAA: preparar datos, ejecutar una acción y verificar el resultado.

Jest aparece para JavaScript, JUnit para Java y xUnit para C#. ESLint apoya la revisión de estilo y algunos errores, pero no decide la arquitectura. Los ejemplos C# utilizan .NET, bibliotecas de clases, interfaces, records, LINQ y DI. Los visuales usan WinForms, eventos, Designer, referencias de proyecto, NuGet y DataGridView. Los ejemplos Node.js usan CommonJS, Express, promesas y adaptadores de MySQL y correo. Java, Python y TypeScript ayudan a mostrar que los patrones conservan su intención entre lenguajes; no todos cuentan con proyectos completos adjuntos.

Git se utiliza para evidenciar la evolución. El manual F05 solicita al menos cuatro commits y presenta mensajes de configuración, refactorización, pruebas y documentación. Ese mínimo pertenece a esa práctica concreta; el requisito del futuro proyecto deberá salir de su enunciado. La rúbrica sugerida del manual distribuye 20 % a diagnóstico, 30 % a refactorización, 20 % a pruebas, 15 % a control de versiones y 15 % a documentación.

La documentación debe permitir instalar, ejecutar y probar el trabajo. El README explica requisitos, estructura y decisiones. Las capturas útiles muestran arquitectura, código que implementa los patrones, operaciones exitosas y validaciones. Las imágenes de resultados esperados deben distinguirse de evidencia obtenida al ejecutar el proyecto entregado.

Las referencias bibliográficas presentes incluyen obras de Gamma y colaboradores, Robert C. Martin, Eric Evans, Vaughn Vernon, Martin Fowler, Freeman y Robson, y Gary McLean Hall, además de documentación técnica de Microsoft, Node.js, Express, Java, Python y TypeScript. Se identificaron como bibliografía incluida en las fuentes; no se realizó una revisión externa completa de esas obras.

## 10. Precisiones técnicas detectadas al contrastar teoría y código

Estas observaciones no invalidan el propósito didáctico del material. Señalan decisiones que habrá que cerrar al construir y probar el proyecto.

1. **Factory Method y fábrica simple.** F10 presenta creadores abstractos y fábricas concretas. En F11, diapositiva 33, `NotificacionFactory.Crear(string)` utiliza un `switch` centralizado. Esta última estructura corresponde a una fábrica simple. Si el proyecto exige Factory Method, se implementará la variación del creador correspondiente y se explicará la diferencia.

2. **Dependencias frente a secuencia visual.** Algunas secuencias de las presentaciones muestran Domain seguido de Infrastructure. Deben interpretarse con el texto que prohíbe referencias del dominio hacia infraestructura. En el proyecto, los diagramas distinguirán llamadas en ejecución y dependencias de código.

3. **Conservar un ejemplo no demuestra equivalencia completa.** F06 mantiene el total 68 para el pedido VIP de Ana. Sin embargo, el código inicial contempla ESTUDIANTE y las estrategias finales mostradas incluyen VIP, MAYORISTA y SinDescuento. También cambian validaciones, tratamiento de mayúsculas y mensajes. Para una refactorización estricta habrá que acordar y comprobar todos los comportamientos que deban conservarse.

4. **Costo de envío en las guías extensas.** En F07, páginas 11–13, y F08, páginas 10–12, la versión inicial suma 10 de envío, mientras el fragmento refactorizado correspondiente no conserva explícitamente ese importe. Al reutilizar el caso habrá que preservar la regla o registrar su modificación como un cambio funcional, según el requisito.

5. **Fragmentos y proyectos completos.** Hay muestras que omiten clases, constructores o métodos para centrarse en un concepto. Por ejemplo, el uso de `LibroBuilder` en F10 incluye pasos que no aparecen todos en la implementación básica mostrada. Los ejemplos de Bridge y Flyweight de F14 también necesitan completar su contexto. La entrega debe reunir código compilable y contratos consistentes.

6. **Singleton y ciclos de vida.** El Singleton sencillo con comprobación de null en F10 no resuelve por sí solo creación concurrente. F13 muestra `Lazy<T>`. Registrar una dependencia como singleton en DI controla su ciclo de vida en el contenedor; no convierte toda construcción directa posible de la clase en una instancia única global.

7. **Invariantes y validaciones completas.** Algunas entidades visuales protegen setters, pero reciben datos sin validar completamente en el constructor. Algunos ejemplos JavaScript mantienen propiedades críticas públicas o solo comprueban que un valor no sea negativo. Para el proyecto será necesario definir tipos y valores permitidos, nulos, conversiones, cantidades, precios y estados, según el dominio.

8. **Persistencia y transacciones.** Las listas en memoria de TECHSTORE no persisten después del cierre. El UoW vacío es un punto didáctico de composición, sin transacción real. Cambiar a una base de datos exigirá implementar guardado del agregado completo y confirmación coherente de los cambios relacionados.

9. **Orden de notificación y guardado.** F11 muestra notificación antes de guardar, mientras otros ejemplos colocan la notificación después de la confirmación. La regla del proyecto debe definir qué significa éxito y cómo responder a fallos parciales para evitar mensajes incompatibles con el estado registrado.

10. **Ejemplos de deshacer y acumulación.** El Command incluido conserva un estado anterior por objeto. Reutilizar el mismo objeto en varias entradas de un historial exige una política adicional para no sobrescribir el estado que corresponde a ejecuciones previas. Los visitantes acumulan importes; una nueva operación debe usar un acumulador nuevo o reiniciarlo conforme al contrato.

11. **Contratos de descuentos.** En F06 las políticas devuelven el subtotal transformado; en F17/F18 `IDescuento.Calcular` devuelve el monto a descontar. Ambas convenciones pueden funcionar, pero mezclarlas produciría un cálculo incorrecto. El contrato del proyecto debe precisar el significado del valor devuelto.

12. **Evidencia de funcionamiento.** Los TXT del ZIP y las imágenes de consola de F15 son referencias para comparar. Los `Main()` muestran una selección de escenarios y no sustituyen las pruebas de aceptación completas. Las carpetas generadas `bin` y `obj` tampoco constituyen evidencia suficiente de que el código final cumple las condiciones.

## 11. Base de trabajo para el futuro proyecto

El proyecto debe comenzar con el problema de negocio y sus reglas. El lenguaje, la interfaz, la persistencia y la cantidad de patrones se concretarán con su enunciado. El predominio de C#/.NET y WinForms en los ejemplos es una referencia didáctica, no una selección automática para cualquier proyecto.

Para cada patrón elegido se preparará una relación verificable: necesidad del sistema, patrón que responde a ella, clases participantes, ubicación arquitectónica, comportamiento que lo utiliza y prueba que demuestra su aporte.

| Decisión | Evidencia que debería producir el proyecto |
|---|---|
| Alcance y reglas | Casos de uso y criterios de aceptación concretos. |
| Modelo | Entidades y operaciones que protejan estados válidos. |
| Arquitectura | Referencias/imports y dependencias coherentes con el estilo elegido. |
| SOLID | Responsabilidades y contratos explicables mediante cambios concretos. |
| Patrones | Participantes activos en los flujos de funcionamiento. |
| Validaciones y errores | Respuesta consistente a entradas inválidas y fallos de operaciones. |
| Persistencia | Comportamiento declarado de guardado, consulta y confirmación. |
| Pruebas | Casos exitosos, negativos y límites que comprueben comportamiento observable. |
| Evolución | Historial Git que registre decisiones y cambios verificables. |
| Entrega | Código, instrucciones, diagramas y evidencia correspondientes a la misma versión. |

La revisión deja preparado un mapa completo del contenido disponible y de sus relaciones. El siguiente desarrollo podrá apoyarse en estas fuentes con criterios de selección, arquitectura y verificación claros, ajustados al enunciado concreto que se defina.
