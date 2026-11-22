# Arquitectura y trazabilidad tecnica

## Requisitos funcionales

| ID | Requisito verificable / caso de uso | Evidencia |
| --- | --- | --- |
| RF01 | Acceso local por hash, rol y cierre de sesion | prueba AccesoHashYPermisosSinInterfaz; capturas 01/20/21 |
| RF02 | CRUD y busqueda de SKU, precio positivo, peso/stock no negativos | pruebas originales de Producto; RestriccionesSqliteReales; CatalogoVista |
| RF03 | Kits anidados, cantidades y ciclos prohibidos | P18, P27, P34; captura 04 |
| RF04 | Clientes activos con categoria editable | ClienteTests; captura 05/06/07 |
| RF05 | Borradores guardados, clonacion y cinco estados undo/redo | P13/P14/P29; EdicionPersistidaTests; capturas 08/09/10 |
| RF06 | Promociones excluyentes y entrega por familia | P02/P03/P04/P12/P32 |
| RF07 | Validacion encadenada y transiciones legales | P05/P08/P09/P23/P31 |
| RF08 | Cobro idempotente y cambio de proveedor | P06/P07/P16/P19/P22/P24; capturas 12/13 |
| RF09 | Guia, seguimiento y entrega por BODEGA | P09/P12/P31; capturas 18/19/25 |
| RF10 | Devolucion total, plazo/motivo, reembolso sin flete | P33/P35; pruebas de limites; capturas 22/23 |
| RF11 | Inventario agrupado por SKU y movimientos referenciados | P02/P05/P07/P35/P36; captura 24 |
| RF12 | Reportes con parser y comprobantes por dos salidas | P17/P25/P26; capturas 26-32 |
| RF13 | Eventos, tres observadores y recuperacion de pendientes | P11/P30/P37; capturas 15/17/41/42 |
| RF14 | Auditoria, metricas unicas y demostraciones reales | P15/P19/P21/P28; capturas 33/37/38/40 |

## Requisitos no funcionales

| ID | Criterio medible | Verificacion |
| --- | --- | --- |
| RNF01 Integridad | Cero stock negativo y cero sobreventas entre dos conexiones | P36 y restricciones SQL |
| RNF02 Mantenibilidad | Cinco proyectos; cero referencias externas en el nucleo | P38, csproj y build limpio |
| RNF03 Usabilidad | Doce vistas; TAB, etiquetas, ErrorProvider, seleccion conservada y confirmaciones | recorrido Windows y validacion de cliente vacio |
| RNF04 Rendimiento | 1000 filas Flyweight con cinco metadatos; medicion equivalente | P21 y captura 37/38; bytes asignados reales |
| RNF05 Seguridad local | Cuatro roles, PBKDF2 salado, denegacion previa al proveedor y su log | P22 y AccesoHashYPermisosSinInterfaz |
| RNF06 Trazabilidad | Cada transicion, pago, movimiento y comando tiene identidad/referencia | P19/P24/P31/P35 y traza UI |
| RNF07 Testabilidad | Al menos 25 pruebas en dominio/aplicacion/integracion; bases aisladas | TRX final y suite P01-P38 |
| RNF08 Recuperabilidad | Seed no destructivo y Outbox reanudado sin repetir efectos | P07/P10/P37; reinicios reales UI |

## Contexto y componentes

```mermaid
flowchart LR
    A[ADMIN / VENTAS / BODEGA / CONSULTA] --> W[Escritorio WinForms]
    W --> U[Casos de uso Application]
    U --> D[Reglas puras Domain]
    U --> P[Puertos Repository / Pago / Eventos / Documentos]
    I[Infrastructure] -. implementa .-> P
    I --> S[(SQLite local)]
    I --> L[Proveedor y avisos simulados locales]
```

Referencias de compilacion: Domain ninguna; Application a Domain; Infrastructure a Application/Domain; WinForms a Application e Infrastructure para composicion; Tests a las tres capas internas. `WinForms/Program.cs` es el unico archivo de presentacion que importa Infrastructure. Los formularios reciben servicios por constructor y se refrescan mediante FormularioPrincipalMediator.

```mermaid
classDiagram
    IElementoCatalogo <|.. ProductoSimple
    IElementoCatalogo <|.. KitCompuesto
    KitCompuesto o-- ComponenteKit
    ComponenteKit --> IElementoCatalogo
    PedidoBuilder --> Pedido
    Pedido *-- LineaPedido
    LineaPedido *-- NodoInstantanea
    Pedido --> EstadoDelPedido
    ConfirmacionPedidoFacade --> IUnidadTrabajoFactory
    ConfirmacionPedidoFacade --> IPasarelaPago
    PagoAutorizadoProxy ..|> IPasarelaPago
    AuditoriaPagoDecorator ..|> IPasarelaPago
    PasarelaLegadaAdapter ..|> IPasarelaPago
    ProcesadorOutbox --> PublicadorEventos
    PublicadorEventos o-- IObservadorEvento
```

## Modelo relacional

```mermaid
erDiagram
    Clientes ||--o{ Pedidos : registra
    Productos ||--o{ KitComponentes : hoja
    Kits ||--o{ KitComponentes : contiene
    Kits o|--o{ KitComponentes : subkit
    Pedidos ||--|{ PedidoDetalles : snapshot
    PedidoDetalles ||--|{ PedidoDetalleComponentes : fisicos
    Productos ||--o{ PedidoDetalleComponentes : sku
    Pedidos ||--|{ HistorialEstados : cambia
    Pedidos ||--o| Pagos : cobra
    Pedidos ||--o{ MovimientosInventario : mueve
    Productos ||--o{ MovimientosInventario : referencia
    Pedidos ||--o| Despachos : despacha
    Pedidos ||--o| Devoluciones : devuelve
    Pedidos ||--o{ EventosOutbox : publica
    EventosOutbox ||--o{ EntregasOutbox : suscriptor
    EventosOutbox ||--o| Notificaciones : avisa
    EventosOutbox ||--o| MetricasEventos : cuenta
```

Usuarios almacena rol y hash. Auditorias guarda comandos, eventos y entrega con correlacion. El historial de transiciones esta normalizado. El arbol JSON de cada linea preserva la estructura anidada; PedidoDetalleComponentes tambien almacena hojas fisicas normalizadas con FK al SKU. No se usa JSON como sustituto de pagos, existencias o relaciones. Los importes son decimal en C#; SQLite los conserva como TEXT decimal y las restricciones numericas usan CAST. Las consultas interpretadas se evaluan sobre objetos tipados, sin SQL concatenado.

## Estados

```mermaid
stateDiagram-v2
    [*] --> BORRADOR
    BORRADOR --> VALIDADO: validar
    BORRADOR --> CANCELADO: cancelar
    VALIDADO --> CANCELADO: cancelar
    VALIDADO --> PAGADO: confirmar cobro
    PAGADO --> DESPACHADO: despacho
    DESPACHADO --> ENTREGADO: entrega
```

La devolucion unica se representa con relacion y bandera Devuelto; no introduce un estado ajeno a la lista oficial. Cada estado es un colaborador con sus acciones validas; no hay un switch central que cambie estados.

## Secuencias

```mermaid
sequenceDiagram
    actor Operador
    participant UI as WinForms
    participant C as Invocador / Command
    participant F as ConfirmacionPedidoFacade
    participant DB as UoW SQLite
    participant P as Proxy / Decorator / Adapter
    participant I as Intentos durables por pedido
    participant O as Outbox / Observers
    Operador->>UI: confirmar
    UI->>C: ConfirmarPedidoCommand
    C->>F: ConfirmarAsync(id, clave)
    F->>DB: BEGIN IMMEDIATE
    F->>DB: consultar idempotencia y stock
    F->>F: Chain y State
    F->>I: reservar clave y recuperar aceptacion por pedido
    alt Aceptacion previa existente
        I-->>F: referencia y proveedor originales
    else Sin aceptacion previa
        F->>P: CobrarAsync(total, clave)
        P-->>F: referencia durable ligada al pedido
    end
    F->>DB: pago + stock + movimientos + estado + evento
    F->>DB: COMMIT
    F-->>UI: pago original
    UI->>O: procesar despues del commit
    O->>DB: efecto y marca por suscriptor
    UI->>UI: Mediator refresca vistas
```

```mermaid
sequenceDiagram
    participant C as Command / Facade
    participant U as UoW
    participant V as StockValidador
    participant P as Proveedor
    C->>U: adquirir escritura
    C->>V: kit teletrabajo x6
    V-->>C: P02 requiere6 disponible5
    C->>U: dispose / rollback
    Note over P: No se invoca
    Note over U: Sin pago, movimiento ni evento de exito
```

## Fuentes tecnicas consultadas

- [Proveedor oficial EF Core SQLite 8.0.34](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite/8.0.34).
- [Microsoft.Data.Sqlite: transacciones y bloqueo de escritores](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions).
- [EF Core: transacciones](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

Los requisitos funcionales se contrastaron con las 20 paginas de `requisitos/GUIA_APE_04.pdf`. Este documento es material tecnico de revision; no sustituye el informe academico aplazado.
