# Estado de construccion

## Estado actual tras la segunda revision: R06

- Inicio comprobado: rama codex/completar-logimarket360, HEAD b2754b0013e000bd8e89d8bc9616d96a15c6e31b, arbol limpio y veinte commits conservados.
- Adjunto Segunda_Revision leido y sus quince archivos originales verificados por SHA-256. Antes de corregir: 149 pruebas aprobadas; externa R06 fallida con dos aceptaciones por 330.00, B PAGADO y A bloqueado por STOCK.
- Corregida la disponibilidad de las aceptaciones pendientes mediante el diario durable y las instantaneas inmutables de VALIDADO, bajo BEGIN IMMEDIATE. A conserva sus unidades; B solo puede vender cantidades libres. Se protegen tambien ajustes negativos. No cambia Domain ni la migracion original.
- Resultado actual: **163/163** (149 conservadas +14 nuevas), **5/5 externas anteriores**, **1/1 externa R06**; P01-P38 nuevamente aprobadas. Build Release -warnaserror con 0 errores/advertencias.
- Extraccion limpia del paquete R06 comprobada con SDK 8.0.420: build Release -warnaserror 0/0 y 163+5+1 pruebas aprobadas; migracion por CLI desde cero y seed doble con datos oficiales sin duplicados. Cien archivos de codigo y dependencias identicos por SHA-256; [verificacion](evidencia/R06-2026-11-22/copia-limpia/verificacion.json).
- Windows real con dos procesos sobre la misma base temporal: A aceptado por 165.00 y rollback, fisico 4 / comprometido 4 / disponible 0; B rechazado antes del proveedor; reinicio de A y otra clave/proveedor recuperan la referencia original; un cargo/pago/movimiento/evento/aviso y fisico 0 / comprometido 0 / disponible 0. Outbox reprocesado dos veces desde la otra ventana sin duplicados.
- Base normal del usuario conservada por SHA-256. No quedan comprobaciones funcionales pendientes de R06; GitHub y el informe academico formal siguen aplazados.
- Commits funcionales R06: bc9b3bf, proteccion durable y regresiones; dbfe6dc, interfaz y verificacion Windows. Los siguientes commits de documentacion y empaquetado quedan registrados en el manifiesto e historial del ZIP actualizado.

Evidencia actual: [RESULTADOS R06](evidencia/R06-2026-11-22/RESULTADOS.md), [P01-P38](evidencia/R06-2026-11-22/P01-P38.md) y [resumen comprobado](evidencia/R06-2026-11-22/resumen.json). Los apartados siguientes conservan el historial de las entregas anteriores.

## Punto inicial comprobado

- Rama main, HEAD 1c6501d; se preservan los cuatro commits existentes.
- Unicos archivos sin seguimiento al recibir el proyecto: docs/requisitos.
- 52/52 pruebas originales ejecutadas; evidencia: evidencia/baseline/baseline.trx.
- PDF original revisado completo: 20 paginas, incluidos 12 wireframes y P01-P38.
- Alcance autorizado: programa, pruebas, evidencia, commits y ZIP. GitHub e informe formal aplazados.

## Secuencia de implementacion

1. Dominio: recorrido de kits, snapshots, calculos, estados, builder, prototype y mementos.
2. SQLite/EF8: modelo relacional, migracion, seed, autenticacion y repositorios.
3. Casos de uso: transacciones, pagos, permisos, comandos, Outbox y devoluciones.
4. WinForms: doce vistas, DI, mediador, salidas y demostraciones.
5. P01-P38, recorrido Windows, reinicio, documentacion y ZIP limpio.

## Entrega anterior comprobada (antes de la revision independiente)

- 23/23 patrones integrados; doce vistas Windows Forms funcionales.
- SQLite/EF Core8.0.34, migracion real, seed idempotente y cuatro cuentas ficticias.
- 109/109 pruebas aprobadas, incluidas P01-P38; conservadas las52 originales.
- Recorrido Windows hasta devolucion: pago59.00, reembolso54.00, persistencia tras reinicios y Outbox recuperado.
- Estado final de la base aislada: un pago, una devolucion, seis movimientos, tres eventos procesados y tres avisos unicos; integridad ok y sin FK rotas.
- Copia limpia del ZIP verificada con SDK8.0.420: restore locked, build Release con advertencias como errores (0/0) y109/109 pruebas; migracion desde cero y seed doble por los comandos del README.
- Las P01-P38 de esa entrega estaban aprobadas. La revision independiente posterior encontro cinco puntos fuera de la cobertura original; se reproducen y corrigen en el apartado siguiente.

## Primera revision R01-R05 (evidencia historica del 22/11/2026)

- Estado inicial real: rama `codex/completar-logimarket360`, HEAD `b494016`, arbol limpio; conservados los doce commits recibidos.
- Se descomprimio y verifico el adjunto; se leyeron LEEME_Revision.md y Prompt_Correcciones_Codex.txt. La peticion del usuario adopta sus cinco correcciones; los requisitos originales permanecen sin cambios.
- Reproduccion: 109/109 originales aprobadas y 5/5 casos adjuntos fallidos. El fallo de seleccion del carrito tambien se reprodujo en la aplicacion Windows anterior, con capturas.
- Corregidos: identidad durable del cobro por pedido y conciliacion antes de reintentar; seed transaccional una sola vez; seleccion de la linea enlazada al ordenar; captura de instantaneas nuevas en Application; tokens validos del Interpreter con posicion de error.
- Resultado de esa revision: **149/149** (109 originales +40 regresiones), **5/5 adjuntas**, **P01-P38 aprobadas**; restore bloqueado y build Release con advertencias como errores, 0/0. Se mantienen .NET8/WinForms/EF Core8 y las capas independientes.
- Windows real: quitar/cambiar tras ordenar, SKU repetidos, Deshacer/Rehacer, guardado/recarga tras reinicio; cobro59 aceptado con fallo local, reinicio, clave/proveedor distintos, misma referencia original, otro reinicio y tercera clave. Estado real: un cargo/pago/aviso, tres movimientos y un evento procesado; integridad `ok`, sin errores FK.
- La base de uso del usuario conserva el SHA-256 inicial; las pruebas usaron bases temporales.
- La extraccion limpia tambien aprueba149/149 y5/5 con SDK8.0.420 y build sin advertencias. Migracion desde cero y seed doble por CLI: datos oficiales sin duplicados, integridad ok. Sus94 archivos de codigo/dependencias coinciden con el codigo entregado; registros en evidencia/correcciones-2026-11-22/copia-limpia.
- No quedan comprobaciones pendientes de los cinco puntos. Los cobros simulados y las limitaciones del diario antiguo se explican en DECISIONES.md. GitHub e informe academico formal siguen aplazados.

| Commit | Correccion comprobada |
| --- | --- |
| a054cdb | Recuperacion de un solo cobro por pedido tras rollback, nuevas claves, proveedor y concurrencia |
| ed26994 | Seed con marca persistente/transaccional; conserva bajas, ediciones y operaciones |
| e207608 | Precios/composicion nuevos desde catalogo; instantaneas historicas conservadas |
| 14639f2 | Parser con tipos de valor y posiciones de error; precedencia/nombres validos |
| c6d9f5f | Carrito enlazado por linea; duplicados, ordenacion, historial, pruebas y capturas |
| 53583a7 | Compatibilidad segura con diarios anteriores: no duplicar aceptaciones sin conciliar |

Detalle de R01-R05: [RESULTADOS.md](evidencia/correcciones-2026-11-22/RESULTADOS.md), [P01-P38.md](evidencia/correcciones-2026-11-22/P01-P38.md) y los TRX/JSON/capturas conservados junto a ellos. La segunda revision encontro R06, fuera de la cobertura anterior; su solucion y resultados actuales constan al inicio de este documento.

## Commits de avance

| Commit | Avance comprobado |
| --- | --- |
| 7c689cc | Contexto original y52 pruebas de partida |
| 4d87265 | Dominio ampliado y61 pruebas |
| 4e05ff1 | Persistencia, transacciones, patrones y108 pruebas |
| 76a1aff | Doce vistas y recorrido real Windows con reinicios |
| f11f10b | Edicion persistida por orden logico,109 pruebas, .sln compatible y dependencias fijadas |
| a643962 | README reproducible, mapas de requisitos, arquitectura, matriz GoF y evidencia Windows |
| b9213bc | Build limpio Release con SDK8,109 pruebas y comparacion SHA-256 de84 archivos de codigo |

Se preservan 88393c6, ce88f5c, 3d747f8 y1c6501d. Los commits posteriores de documentacion/evidencia se incluyen en HISTORIAL_COMMITS.txt al empaquetar. No se creo remoto ni se hizo push. El informe academico formal esta aplazado.

Los resultados detallados se encuentran en [ACEPTACION.md](evidencia/ACEPTACION.md), [RECORRIDO_WINDOWS.md](evidencia/RECORRIDO_WINDOWS.md), [PATRONES.md](PATRONES.md) y [DECISIONES.md](DECISIONES.md).
