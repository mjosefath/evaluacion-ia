# 02 — Productos, movimientos, historial y reporte

## Alcance

Fuente: `recursos/Evaluacion IA.pdf`, secciones 1 a 6 (documento de contexto del dueño).

> Construir una aplicación web completa y sencilla para registrar productos, gestionar entradas, salidas y ajustes,
> consultar el historial y emitir un reporte. […] Se espera un modelo mínimo, suficiente y normalizado […]
> aproximadamente 10 campos o menos por tabla de negocio, salvo justificación técnica.

Requerimientos que cubre: RF03 (menú), RF04 (productos), RF05 (entrada), RF06 (salida), RF07 (ajuste),
RF08 (historial) y RF09 (reporte). RF01 y RF02 (sesión y seguridad) ya están resueltos en `01-estructura-inicial`.

Fuera: pantalla de mantenimiento de categorías (el contexto la excluye), exportar a Excel/PDF, usuarios y roles
adicionales, contenido de la página de Inicio, publicación y pruebas.

## Propósito

Saber en todo momento cuántas unidades hay de cada producto y cuánto valen, con la garantía de que cada unidad se
explica por un movimiento registrado: quién lo hizo, cuándo y cuál fue la existencia antes y después.

## Estándar de tablas (BD Sistema)

Aplica a **todas** las tablas de la BD Sistema: las de este documento, las de `01` y las de módulos futuros.
La BD Identity no entra: conserva el esquema estándar de ASP.NET Identity.

### Dos tipos de tabla

| Tipo | Qué es | Ejemplos |
|------|--------|----------|
| Mutable | Se crea y se edita; nunca se borra físicamente | Products, Categories, UserProfiles, Permissions |
| Inmutable | Solo se inserta; nunca se edita ni se borra | InventoryMovements, AccessLogs |

### Columnas obligatorias

| Columna | Mutable | Inmutable | Tipo / regla |
|---------|:-------:|:---------:|--------------|
| `Id` | ✔ | ✔ | `int IDENTITY(1,1)` PK; `bigint IDENTITY` solo en tablas de alto volumen (movimientos, bitácoras) |
| `StatusId` | ✔ | — | `tinyint` FK → `RecordStatuses` (1 = Activo, 2 = Inactivo), default 1. Es el **borrado lógico** |
| `CreatedAtUtc` | ✔ | ✔ | `datetime2(3)`, default `SYSUTCDATETIME()` |
| `CreatedBy` | ✔ | ✔ | `int` FK → `UserProfiles.Id`. Solo admite NULL en filas que crea el sistema (seed de catálogos, intento de login sin usuario) |
| `UpdatedAtUtc` | ✔ | — | `datetime2(3)`, NULL hasta la primera edición |
| `UpdatedBy` | ✔ | — | `int` FK → `UserProfiles.Id`, NULL hasta la primera edición |
| `RowVersion` | ✔ | — | `rowversion`; token de concurrencia optimista en toda edición |

En las inmutables, `CreatedAtUtc` y `CreatedBy` son la fecha del evento y su responsable; no hay columnas de
fecha propias por tabla (nada de `OccurredAtUtc`, `FechaMovimiento`, etc.).

### Reglas del estándar

- E1. Ninguna tabla se borra físicamente desde la aplicación. Baja = `StatusId = 2`; alta de nuevo = `StatusId = 1`.
- E2. Las FK son `NO ACTION`: nada de `ON DELETE CASCADE`.
- E3. Las unicidades de negocio (por ejemplo, el código de producto) aplican también a los registros inactivos.
- E4. Toda fecha se guarda en UTC con sufijo `Utc`; la Web la muestra en hora local.
- E5. La API llena `CreatedAtUtc`, `CreatedBy`, `UpdatedAtUtc` y `UpdatedBy` con el usuario de la sesión; la Web
  nunca los envía.
- E6. Toda edición envía el `RowVersion` leído; si no coincide, la API responde 409.
- E7. Las listas muestran solo activos por defecto, con filtro para ver inactivos o todos.
- E8. Tipos: dinero `decimal(18,2)`; cantidades `int`; textos `nvarchar` con longitud fija (sin `max`); códigos
  internos de catálogo `varchar`.
- E9. Nombres en inglés y PascalCase; tablas en plural; PK `Id`; FK `<Entidad>Id`. Restricciones: `PK_<Tabla>`,
  `FK_<Tabla>_<Referencia>`, `UX_<Tabla>_<Columnas>` (único), `IX_<Tabla>_<Columnas>`, `CK_<Tabla>_<Regla>`,
  `DF_<Tabla>_<Columna>`.
- E10. Catálogos: `Name` único; llevan además `Code` (`varchar`, único) solo cuando el código del sistema decide
  algo según su valor (tipos de movimiento, estatus).
- E11. Procedimientos almacenados: `usp_<Entidad>_<Accion>` (por ejemplo, `usp_Products_List`). Vistas:
  `vw_<Nombre>`. Se crean en los scripts con `CREATE`, como las tablas.

## Búsqueda maestra, paginación y consultas por procedimiento almacenado

Aplica a todos los listados y reportes: los de este documento y los de módulos futuros.

### Búsqueda maestra

- B1. Cada listado tiene **una sola caja de búsqueda** que busca en todas las columnas visibles de la tabla.
- B2. El texto se separa por espacios y **cada palabra** debe aparecer en alguna columna (todas las palabras se
  combinan con Y). Ejemplo: `mouse ele` encuentra "Mouse USB · Electrónica".
- B3. Coincidencia parcial ("contiene"), sin distinguir mayúsculas ni acentos.
- B4. Si una palabra es un número, también busca coincidencias exactas en las columnas numéricas visibles
  (cantidades, existencias y precios).
- B5. La búsqueda maestra se combina (con Y) con los filtros específicos de la pantalla (categoría, tipo, fechas,
  estatus); no los reemplaza.
- B6. Los comodines que escribe el usuario (`%`, `_`, `[`) se tratan como texto normal, y todo valor llega al
  procedimiento como parámetro (nunca se arma SQL concatenando texto).

| Pantalla | Columnas donde busca |
|----------|----------------------|
| Productos | Código, nombre, categoría, estatus, estado de stock; numéricas: precio, mínimo, existencia |
| Historial | Código y nombre del producto, tipo, motivo, responsable; numéricas: efecto, existencia anterior y posterior |
| Reporte | Código, producto, categoría, estado de stock; numéricas: precio, existencia, mínimo |

En el Reporte, la búsqueda maestra cubre el filtro "por código o nombre" que pide el contexto.

### Paginación

- PG1. Siempre en el servidor: nunca se envían a la Web todas las filas.
- PG2. Parámetros: `page` (desde 1) y `pageSize` (20 por defecto; opciones 20, 50 y 100; máximo 100).
- PG3. Orden: cada pantalla tiene un orden por defecto, y el usuario puede ordenar por las columnas permitidas
  (lista cerrada por pantalla, ascendente o descendente). Un valor fuera de la lista se ignora y se usa el orden
  por defecto.
- PG4. Respuesta paginada estándar (en Contracts): `Items`, `Page`, `PageSize`, `TotalRows` y `TotalPages`. El
  Reporte agrega `GrandTotal`.
- PG5. Los totales (`TotalRows` y, en el Reporte, `GrandTotal`) se calculan sobre **todas** las filas filtradas,
  no solo sobre la página.
- PG6. Al cambiar la búsqueda o un filtro, se regresa a la página 1.
- PG7. Los filtros, la página y el orden viajan en la URL: recargar la página, usar Atrás o compartir el enlace
  conserva la vista.

| Pantalla | Orden por defecto | Columnas ordenables |
|----------|-------------------|---------------------|
| Productos | Código ↑ | Código, nombre, categoría, precio, existencia |
| Historial | Fecha ↓ | Fecha, producto, tipo |
| Reporte | Código ↑ | Código, producto, categoría, existencia, valor |

### Consultas por procedimiento almacenado

- SP1. Los listados y el reporte se leen con procedimientos almacenados; las escrituras (alta, edición,
  movimientos) siguen en la API dentro de su transacción.
- SP2. Procedimientos de este documento:

| Procedimiento | Pantalla | Parámetros (además de `@Search`, `@Page`, `@PageSize`, `@SortColumn`, `@SortDirection`) | Salida |
|---------------|----------|-------------------------------------------------|--------|
| `usp_Products_List` | Productos | `@StatusId` (NULL = todos) | Filas + `@TotalRows OUTPUT` |
| `usp_InventoryMovements_List` | Historial | `@MovementTypeId`, `@DateFromUtc`, `@DateToUtc` | Filas + `@TotalRows OUTPUT` |
| `usp_Report_Stock` | Reporte | `@CategoryId` | Filas + `@TotalRows OUTPUT` + `@GrandTotal OUTPUT` |

- SP3. La existencia actual sale de una sola vista, `vw_ProductStock` (ProductId y existencia = último
  `StockAfter`, o 0 si no hay movimientos). La usan los tres procedimientos y la API al registrar un movimiento
(dentro del bloqueo de R8); nadie más la recalcula.
- SP4. Los totales van en parámetros `OUTPUT`, así llegan aunque la página pedida no tenga filas.
- SP5. El orden se arma con un `CASE` sobre la lista cerrada de PG3; no se usa SQL dinámico.
- SP6. La API llama a los procedimientos y transforma el resultado en el DTO paginado; la Web no sabe que existen.

## Entidades y datos

Columnas de negocio de cada tabla. Las columnas del estándar se indican por tipo y no se repiten.

### RecordStatuses (catálogo del sistema, mutable)

| Campo | Tipo / notas |
|-------|--------------|
| Code | `varchar(10)`, único: `ACTIVO`, `INACTIVO` |
| Name | `nvarchar(20)`, único: Activo, Inactivo |

Seed: 1 = Activo, 2 = Inactivo. Cubre el catálogo "Estado del producto" del contexto, y lo usan todas las tablas
mutables. Su propio `StatusId` siempre es 1.

### Categories (catálogo, mutable)

| Campo | Tipo / notas |
|-------|--------------|
| Name | `nvarchar(50)`, único |

Seed: `Electrónica`, `Papelería`, `Limpieza`. Sin pantalla de mantenimiento.

### MovementTypes (catálogo del sistema, mutable)

| Campo | Tipo / notas |
|-------|--------------|
| Code | `varchar(10)`, único: `ENTRADA`, `SALIDA`, `AJUSTE` |
| Name | `nvarchar(30)`: Entrada, Salida, Ajuste |

### Products (mutable)

| Campo | Tipo / notas |
|-------|--------------|
| Code | `nvarchar(20)`, único (E3), guardado en mayúsculas y sin espacios en los extremos |
| Name | `nvarchar(150)` |
| CategoryId | FK → Categories |
| UnitPrice | `decimal(18,2)`, `CK_Products_UnitPrice` ≥ 0 |
| MinStock | `int`, `CK_Products_MinStock` ≥ 0 |

Total: 5 de negocio + 7 del estándar = 12. Los 7 son técnicos (llave, borrado lógico, auditoría y concurrencia),
y con eso queda la justificación que pide el contexto para pasar de 10.
**No hay columna de existencia**: la existencia actual sale del último movimiento (ver decisiones técnicas).

### InventoryMovements (inmutable, `bigint`)

| Campo | Tipo / notas |
|-------|--------------|
| ProductId | FK → Products |
| MovementTypeId | FK → MovementTypes |
| Quantity | `int` con signo = efecto sobre la existencia; `CK` ≠ 0 |
| StockBefore | `int`, `CK` ≥ 0 |
| StockAfter | `int`, `CK` ≥ 0 y `CK` `StockAfter = StockBefore + Quantity` |
| Reason | `nvarchar(250)`, NULL (obligatorio en AJUSTE; lo valida la API) |

Fecha y responsable = `CreatedAtUtc` y `CreatedBy`. Total: 6 + 3 = 9.
Índices: `IX_InventoryMovements_ProductId_Id (ProductId, Id DESC) INCLUDE (StockAfter)` para obtener la
existencia actual; `IX_InventoryMovements_CreatedAtUtc (CreatedAtUtc DESC)` para el historial.

### Datos calculados (no se guardan)

| Dato | Cálculo |
|------|---------|
| Existencia actual | `StockAfter` del último movimiento del producto; 0 si no tiene movimientos |
| Estado de stock | `BAJO` si existencia ≤ MinStock; `NORMAL` en otro caso |
| Valor del inventario | `UnitPrice × existencia`, redondeado a 2 decimales |
| Total general | Suma del valor de las filas que incluyen los filtros |

### Seed (por script, solo INSERT)

1. Catálogos: `RecordStatuses`, `Categories` y `MovementTypes`, con `CreatedBy = NULL` (sistema).
2. Los 5 productos (ELE-001, ELE-002, PAP-001, PAP-002, LIM-001) con la categoría, el precio y el mínimo del
   contexto. Al no tener movimientos, su existencia es cero.
3. Un movimiento ENTRADA por producto: `StockBefore = 0`, `Quantity = StockAfter =` 20, 12, 30, 8 y 15,
   `Reason = 'Carga inicial'`.
4. En los productos y movimientos del seed, `CreatedBy` = perfil del usuario de prueba.

### Alineación de las tablas de `01` al estándar

Ya están construidas y hoy no cumplen el estándar. Lo que cambia en cada una:

| Tabla | Cambio |
|-------|--------|
| UserProfiles | `Id` de `uniqueidentifier` a `int IDENTITY`; `IsActive` → `StatusId`; `CreatedByIdentityUserId` / `ModifiedByIdentityUserId` → `CreatedBy` / `UpdatedBy` (`int`, FK a sí misma); `ModifiedAtUtc` → `UpdatedAtUtc`; agregar `RowVersion`. `IdentityUserId` se queda como vínculo con Identity. |
| Permissions | Agregar `StatusId`, columnas de auditoría y `RowVersion`. |
| RolePermissions | Agregar `Id IDENTITY` con `UX (RoleName, PermissionId)`; agregar `StatusId`, auditoría y `RowVersion`; quitar `ON DELETE CASCADE` (E2). Revocar un permiso = `StatusId = 2`. |
| AccessLogs | `OccurredAtUtc` → `CreatedAtUtc`; `IdentityUserId` → `CreatedBy` (`int` FK, NULL si el login falla sin usuario). `UserNameAttempted` se queda. |

## Flujos

### F1 — Productos (RF04)

1. Listado: búsqueda maestra (B1–B6), filtro de estatus (E7) y paginación en el servidor (PG1–PG7), con
   `usp_Products_List`. Columnas: código, nombre, categoría, precio, mínimo, existencia, estado de stock y estatus.
2. Alta: código, nombre, categoría, precio y mínimo. Nace Activo y con existencia 0.
3. Edición: nombre, categoría, precio y mínimo, con `RowVersion` (E6). El código y la existencia se muestran, pero
   no se pueden editar.
4. Desactivar / reactivar: con confirmación, sin salir del listado. Si el producto tiene existencia distinta de 0,
   no se desactiva y se avisa: "No se puede desactivar: el producto tiene N unidades. Llévalo a 0 con un ajuste."

### F2 — Registrar movimiento (RF05, RF06, RF07)

Una sola pantalla de captura (Movimientos), pensada para usarse con teclado:

1. El usuario elige el tipo: Entrada / Salida / Ajuste.
2. Escribe el código o el nombre del producto; solo aparecen productos activos. Al elegirlo ve su existencia actual.
3. Captura la cantidad: positiva en Entrada y Salida; con signo en Ajuste (variación, por ejemplo `-3`).
4. Motivo: opcional en Entrada y Salida, obligatorio en Ajuste.
5. Guarda. La API, en una sola transacción:
   1. bloquea la fila del producto (`UPDLOCK`) y verifica que esté activo;
   2. lee la existencia actual (último `StockAfter`);
   3. calcula la existencia posterior y la rechaza si queda negativa;
   4. inserta el movimiento con la existencia anterior y la posterior; `CreatedAtUtc` y `CreatedBy` los pone la
      API (E5).
6. La Web muestra un aviso de éxito con la existencia anterior y la posterior, y deja el formulario listo para
   la siguiente captura conservando el tipo elegido.

### F3 — Historial (RF08)

Listado del más reciente al más antiguo, con `usp_InventoryMovements_List` y paginación en el servidor. Columnas:
fecha y hora local, código y nombre del producto, tipo, efecto con signo (+20, −3), existencia anterior,
existencia posterior, motivo y responsable.
Filtros: búsqueda maestra, tipo y rango de fechas (fechas locales convertidas a UTC por la API).

### F4 — Reporte de existencias (RF09)

Columnas, en este orden: Código, Producto, Categoría, Precio unitario, Existencia actual, Stock mínimo,
Estado de stock y Valor del inventario. Se lee con `usp_Report_Stock`, paginado en el servidor. Filtros: categoría y
búsqueda maestra (incluye código y nombre). Total general al pie, calculado sobre todas las filas filtradas y no
solo sobre la página visible (PG5). Importes con 2 decimales. Solo productos activos: por R12, un inactivo siempre
tiene existencia 0 y no aporta valor.

## Reglas de negocio

- R1. El código de producto es único entre todos los productos, activos e inactivos, sin distinguir mayúsculas.
- R2. Un producto nuevo empieza con existencia 0. La existencia no se captura ni se edita en Productos.
- R3. Entrada y Salida: cantidad entera mayor que 0.
- R4. Ajuste: variación entera distinta de 0, positiva o negativa, con motivo obligatorio.
- R5. Ninguna operación puede dejar la existencia en negativo.
- R6. Un producto inactivo no admite ningún movimiento.
- R7. Los movimientos nunca se editan ni se borran; guardan el usuario, la fecha y la existencia antes y después.
- R8. El movimiento y la nueva existencia se registran en la misma transacción, con un bloqueo por producto, para
  evitar inconsistencias cuando llegan solicitudes simultáneas.
- R9. Precio unitario y stock mínimo no pueden ser negativos; los importes van con 2 decimales.
- R10. Estado de stock: BAJO si existencia ≤ mínimo; NORMAL en otro caso.
- R11. Valor = precio × existencia; el total general suma solo las filas filtradas.
- R12. Solo se puede desactivar un producto con existencia 0; si no está en 0, se rechaza con un aviso que indica
  la existencia actual. La validación es de la API, dentro del mismo bloqueo por producto de R8.

## Pantallas

Menú (RF03), en este orden: Inicio, Productos, Movimientos, Historial, Reporte y Cerrar sesión. Usa el layout y los
iconos ya construidos (`_Sidebar`, ADR-003, ADR-004).

| Pantalla | Qué ve y qué hace |
|----------|-------------------|
| Productos | Tabla con búsqueda y filtro. Alta y edición en el modal de acción existente (`_ActionModal`), sin cambiar de página. Desactivar con `_Confirm`. |
| Movimientos | Formulario de captura en un solo panel, con foco inicial en el producto, Enter para guardar, aviso con la existencia resultante y el formulario se limpia (excepto el tipo). |
| Historial | Tabla con filtros y paginación; efecto con signo. |
| Reporte | Filtros arriba, tabla, total general fijo al pie y estado de stock como badge. |

En todas: validaciones bajo cada campo, avisos de éxito o error con `_Alerts` y tablas con desplazamiento
horizontal dentro de su contenedor en teléfono (sin desbordar la página).

En todos los listados:
- La caja de búsqueda maestra está arriba de la tabla; busca al presionar Enter o 300 ms después de dejar de
  escribir, y tiene un botón para limpiarla.
- Los filtros específicos van junto a la búsqueda.
- El pie de la tabla muestra "Mostrando 21–40 de 57", los botones Anterior y Siguiente, los números de página y el
  selector de tamaño de página (20, 50, 100).
- Al hacer clic en el encabezado de una columna ordenable, cambia el orden y se muestra la flecha de dirección.
- Sin resultados: mensaje "No hay resultados para «texto»" con un botón para limpiar filtros.
- Mientras carga: `_Loading` sobre la tabla, sin borrar la página que ya se ve.

## Permisos

Todos asignados al rol `Administrador` en el seed.

| Pantalla | Código |
|----------|--------|
| Productos | `productos.ver`, `productos.crear`, `productos.editar`, `productos.activar` (desactivar y reactivar) |
| Movimientos | `movimientos.entrada`, `movimientos.salida`, `movimientos.ajuste` |
| Historial | `historial.ver` |
| Reporte | `reporte.ver` |

## API (estructura)

| Método | Ruta | Permiso |
|--------|------|---------|
| GET | `/api/categories` | autenticado |
| GET | `/api/products?search&statusId&page&pageSize&sort&dir` | productos.ver |
| GET | `/api/products/{id}` (incluye `RowVersion`) | productos.ver |
| GET | `/api/products/lookup?term` (solo activos, con existencia) | cualquiera de movimientos.* |
| POST | `/api/products` | productos.crear |
| PUT | `/api/products/{id}` (con `RowVersion`) | productos.editar |
| POST | `/api/products/{id}/deactivate` · `/activate` (con `RowVersion`) | productos.activar |
| POST | `/api/movements` (`type`, `productId`, `quantity`, `reason`) | movimientos.{tipo} |
| GET | `/api/movements?search&typeId&from&to&page&pageSize&sort&dir` | historial.ver |
| GET | `/api/reports/stock?search&categoryId&page&pageSize&sort&dir` (página + `TotalRows` + `GrandTotal`) | reporte.ver |

`/api/products/lookup` no se pagina: devuelve como máximo 10 productos activos para el autocompletado de
Movimientos, con la misma regla de búsqueda (B2–B3) sobre el código y el nombre.

Errores (formato de `01`): código duplicado → 409; existencia insuficiente, producto inactivo, cantidad o motivo
inválidos → 400 con el error en su campo (por ejemplo, "Existencia insuficiente: disponible 8"); desactivar con
existencia ≠ 0 → 400 con la existencia actual en el mensaje; `RowVersion` distinto → 409 "El producto cambió,
recarga".

## Reglas que impone a otros módulos

- Toda tabla nueva de la BD Sistema cumple el estándar (E1–E11) y se declara mutable o inmutable.
- Todo listado o reporte nuevo usa búsqueda maestra (B1–B6), paginación en el servidor (PG1–PG7) y un procedimiento
  almacenado de lectura (SP1–SP6), y declara sus columnas de búsqueda y de orden.
- Cualquier cambio futuro de existencia debe pasar por `InventoryMovements` con el mismo procedimiento transaccional.
- Nadie calcula la existencia por su cuenta: se usa una sola consulta o vista en la API.

## Decisiones técnicas tomadas

| Decisión | Por qué |
|----------|---------|
| Estándar único de columnas (E1–E10) para toda la BD Sistema | Mismo patrón de llave, borrado lógico, auditoría, fechas y concurrencia en todas las tablas; el ejecutor copia un solo molde. |
| Borrado lógico con `StatusId` → `RecordStatuses` y no con un `bit` | Cumple el catálogo "Activo / Inactivo" que el contexto pide insertar por script, y es el mismo para todas las tablas. |
| `int IDENTITY` como llave (y `bigint` en tablas de alto volumen) | Uniforme, compacta e indexable; `IdentityUserId` sigue siendo solo el vínculo con Identity. |
| `rowversion` como token de concurrencia | Lo mantiene SQL Server, sin depender de la precisión de una fecha; un movimiento no modifica la fila del producto, así que no provoca conflictos falsos. |
| La existencia se deriva del último `StockAfter` y no se guarda en Products | Una sola fuente de verdad que siempre cuadra con el historial, y el seed queda solo con INSERT. |
| `UPDLOCK` sobre la fila del producto dentro de la transacción | Serializa los movimientos del mismo producto sin bloquear a los demás. |
| `Quantity` con signo, más CHECK de `StockAfter = StockBefore + Quantity` | El historial muestra el efecto directo y la BD rechaza filas incoherentes. |
| El código no se edita después del alta | Es el identificador del producto en el historial y en el reporte. |
| Se permite reactivar un producto | Corregir una desactivación por error no obliga a crear un código nuevo, que además no se podría repetir (R1). |
| Las tablas de `01` se alinean al estándar | Sin eso habría dos patrones en la misma BD; los cambios están listados en "Alineación". |
| Búsqueda maestra por palabras (todas deben aparecer) en todas las columnas visibles | Permite "buscar cualquier cosa" y además acotar escribiendo más palabras. |
| Los procedimientos solo leen; las escrituras se quedan en la API | La lógica de movimientos (bloqueo, validación, auditoría) vive en un solo lugar. |
| Totales en parámetros `OUTPUT` | Llegan aunque la página esté vacía, y el total general no depende de la página. |
| Orden con `CASE` sobre una lista cerrada, sin SQL dinámico | Evita inyección de SQL y mantiene un plan de ejecución estable. |
| Búsqueda con "contiene" (`LIKE '%…%'`), sin índice de texto completo | Con el volumen esperado basta; si crece, se evalúa el índice de texto completo sin cambiar el contrato. |
| Filtros, página y orden en la URL | Recargar o volver no pierde la vista del usuario. |

## Preguntas de negocio pendientes

Ninguna.

## Decisiones del dueño

| Fecha | Decisión |
|-------|----------|
| 2026-10-14 | El alcance, los datos, los catálogos, el seed, las reglas y las columnas del reporte se toman de `recursos/Evaluacion IA.pdf`, secciones 1–6. |
| 2026-10-14 | Primera versión: Entrada, Salida y Ajuste; un solo almacén, sin transferencias ni ubicaciones; sin proveedores ni clientes. |
| 2026-10-14 | No se permite existencia negativa; los movimientos afectan la existencia al guardarse, sin borrador. |
| 2026-10-14 | Un producto con existencia distinta de 0 no se puede desactivar; se avisa al usuario y se rechaza (R12). |
| 2026-10-14 | La forma de los scripts de entrega y el alta del usuario de prueba no son temas del analista; quedan fuera de este documento. |
| 2026-10-14 | Todas las tablas deben seguir un estándar uniforme de llave identity, borrado lógico, auditoría de creación y modificación, y fechas. |
| 2026-10-14 | Los filtros son maestros: se puede buscar cualquier cosa desde una sola caja. |
| 2026-10-14 | Los reportes se consultan con procedimientos almacenados y con paginación. |

---
Fuera de punto: el contexto también exige `PROMPTS.md` y que el README liste lo que quede incompleto; no está en ningún documento todavía.
