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

## Resultado comprobado

- 23/23 patrones integrados; doce vistas Windows Forms funcionales.
- SQLite/EF Core8.0.34, migracion real, seed idempotente y cuatro cuentas ficticias.
- 109/109 pruebas aprobadas, incluidas P01-P38; conservadas las52 originales.
- Recorrido Windows hasta devolucion: pago59.00, reembolso54.00, persistencia tras reinicios y Outbox recuperado.
- Estado final de la base aislada: un pago, una devolucion, seis movimientos, tres eventos procesados y tres avisos unicos; integridad ok y sin FK rotas.
- Copia limpia del ZIP verificada con SDK8.0.420: restore locked, build Release con advertencias como errores (0/0) y109/109 pruebas; migracion desde cero y seed doble por los comandos del README.
- Sin comprobaciones funcionales pendientes de P01-P38. ZIP limpio con manifiesto SHA-256 e historial de commits; codigo comparado contra la copia limpia probada.

## Commits de avance

| Commit | Avance comprobado |
| --- | --- |
| 7c689cc | Contexto original y52 pruebas de partida |
| 4d87265 | Dominio ampliado y61 pruebas |
| 4e05ff1 | Persistencia, transacciones, patrones y108 pruebas |
| 76a1aff | Doce vistas y recorrido real Windows con reinicios |
| f11f10b | Edicion persistida por orden logico,109 pruebas, .sln compatible y dependencias fijadas |
| a643962 | README reproducible, mapas de requisitos, arquitectura, matriz GoF y evidencia Windows |

Se preservan 88393c6, ce88f5c, 3d747f8 y1c6501d. Los commits posteriores de documentacion/evidencia se incluyen en HISTORIAL_COMMITS.txt al empaquetar. No se creo remoto ni se hizo push. El informe academico formal esta aplazado.

Los resultados detallados se encuentran en [ACEPTACION.md](evidencia/ACEPTACION.md), [RECORRIDO_WINDOWS.md](evidencia/RECORRIDO_WINDOWS.md), [PATRONES.md](PATRONES.md) y [DECISIONES.md](DECISIONES.md).
