# Análisis — Sistema de inventario

Índice de documentos de análisis. El dueño entrega puntos; cada uno produce un archivo `NN-<tema>.md`.

| Nº | Documento | Alcance | Estado |
|----|-----------|---------|--------|
| 01 | [01-estructura-inicial.md](01-estructura-inicial.md) | Solución .NET 10 (API + Web + contratos + cliente + datos), dos BD, sesión cookie/JWT, roles/permisos (estructura), configuración, auditoría base, layout UI, errores, README de arranque. Sin módulos de inventario. | Aprobado |
| 02 | [02-productos-movimientos-reporte.md](02-productos-movimientos-reporte.md) | Estándar de tablas de la BD Sistema (identity, borrado lógico, auditoría, fechas, concurrencia) y su alineación con las de 01; búsqueda maestra, paginación y procedimientos almacenados de lectura; entidades, flujos, reglas y permisos de Productos, Movimientos (entrada/salida/ajuste), Historial y Reporte, según el contexto (PDF, secciones 1–6). | Aprobado |
| 03 | [03-errores-avisos-validaciones.md](03-errores-avisos-validaciones.md) | Estándar único de errores (códigos, contrato de la API, mapeo), mensajes, validaciones por capa, avisos, confirmaciones, carga y logs, aplicado a todo lo construido (01) y lo analizado (02), con la lista de alineación. | Aprobado |
