# Estado de construccion

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

## Correcciones verificadas el 22/11/2026

- Estado inicial real: rama `codex/completar-logimarket360`, HEAD `b494016`, arbol limpio; conservados los doce commits recibidos.
- Se descomprimio y verifico el adjunto; se leyeron LEEME_Revision.md y Prompt_Correcciones_Codex.txt. La peticion del usuario adopta sus cinco correcciones; los requisitos originales permanecen sin cambios.
- Reproduccion: 109/109 originales aprobadas y 5/5 casos adjuntos fallidos. El fallo de seleccion del carrito tambien se reprodujo en la aplicacion Windows anterior, con capturas.
- Corregidos: identidad durable del cobro por pedido y conciliacion antes de reintentar; seed transaccional una sola vez; seleccion de la linea enlazada al ordenar; captura de instantaneas nuevas en Application; tokens validos del Interpreter con posicion de error.
- Resultado actual: **149/149** (109 originales +40 regresiones), **5/5 adjuntas**, **P01-P38 aprobadas**; restore bloqueado y build Release con advertencias como errores, 0/0. Se mantienen .NET8/WinForms/EF Core8 y las capas independientes.
- Windows real: quitar/cambiar tras ordenar, SKU repetidos, Deshacer/Rehacer, guardado/recarga tras reinicio; cobro59 aceptado con fallo local, reinicio, clave/proveedor distintos, misma referencia original, otro reinicio y tercera clave. Estado real: un cargo/pago/aviso, tres movimientos y un evento procesado; integridad `ok`, sin errores FK.
- La base de uso del usuario conserva el SHA-256 inicial; las pruebas usaron bases temporales.
- No quedan comprobaciones pendientes de los cinco puntos. Los cobros simulados y las limitaciones del diario antiguo se explican en DECISIONES.md. GitHub e informe academico formal siguen aplazados.

| Commit | Correccion comprobada |
| --- | --- |
| a054cdb | Recuperacion de un solo cobro por pedido tras rollback, nuevas claves, proveedor y concurrencia |
| ed26994 | Seed con marca persistente/transaccional; conserva bajas, ediciones y operaciones |
| e207608 | Precios/composicion nuevos desde catalogo; instantaneas historicas conservadas |
| 14639f2 | Parser con tipos de valor y posiciones de error; precedencia/nombres validos |
| c6d9f5f | Carrito enlazado por linea; duplicados, ordenacion, historial, pruebas y capturas |
| 53583a7 | Compatibilidad segura con diarios anteriores: no duplicar aceptaciones sin conciliar |

Detalle actual: [RESULTADOS.md](evidencia/correcciones-2026-11-22/RESULTADOS.md), [P01-P38.md](evidencia/correcciones-2026-11-22/P01-P38.md) y los TRX/JSON/capturas conservados junto a ellos. El manifiesto del ZIP registra el HEAD completo y su historial, incluidos los commits posteriores de evidencia.

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
