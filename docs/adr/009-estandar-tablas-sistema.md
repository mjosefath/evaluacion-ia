# ADR-009 — Estándar de tablas de la BD Sistema (E1–E11)

**Estado:** Aceptado (2026-10-14)

## Contexto

El análisis 02 define un molde único de columnas y reglas (E1–E11) para toda la BD Sistema. Las tablas de `01` (UserProfiles, Permissions, RolePermissions, AccessLogs) no lo cumplían. Se autorizó recrear la BD con scripts CREATE y alinear el código (T02-05). Categories/MovementTypes/Products quedan para tareas siguientes; este ADR fija el molde.

## Decisión

| Tema | Decisión |
|------|----------|
| Tipos de tabla | **Mutable** (se edita; baja lógica) vs **Inmutable** (solo insert; p. ej. AccessLogs, movimientos). |
| Llave | `int IDENTITY` PK; `bigint IDENTITY` en alto volumen (AccessLogs, movimientos). |
| Borrado lógico | `StatusId` `tinyint` FK → `RecordStatuses` (1 = Activo, 2 = Inactivo). Solo mutables. |
| Auditoría mutable | `CreatedAtUtc`, `CreatedBy`, `UpdatedAtUtc`, `UpdatedBy`, `RowVersion`. |
| Auditoría inmutable | Solo `CreatedAtUtc` + `CreatedBy` (fecha/responsable del evento). |
| Fechas | `datetime2(3)` UTC, sufijo `Utc`. |
| FK | `NO ACTION` (nada de `ON DELETE CASCADE`). |
| Nombres | Inglés PascalCase; plural; restricciones `PK_` / `FK_` / `UX_` / `IX_` / `DF_` (E9). |
| RecordStatuses | Catálogo de sistema: `Id` tinyint, `Code` varchar único, `Name`; seed Activo/Inactivo. Sin `StatusId` propio. |
| UserProfiles | `Id` int; `IdentityUserId` (vínculo Identity, UX); `StatusId`; auditoría + `RowVersion`; `CreatedBy`/`UpdatedBy` FK self. |
| Permissions / RolePermissions | Mutables con estándar completo; RolePermissions tiene `Id` + UX `(RoleName, PermissionId)`; revocar = `StatusId = 2`. |
| AccessLogs | Inmutable; `CreatedBy` = `UserProfiles.Id` (NULL si no hay perfil). Sin `IdentityUserId` ni `OccurredAtUtc`. |
| Scripts | Solo CREATE/INSERT vírgenes bajo `database/system/`; recrear BD ante cambio de esquema. |

## Alternativas descartadas

| Alternativa | Por qué no |
|-------------|------------|
| `IsActive` bit por tabla | Dos patrones; el contexto pide catálogo Activo/Inactivo. |
| CASCADE en RolePermissions | Viola E2; baja lógica con StatusId. |
| GUID como PK de UserProfiles | Rompe el molde int IDENTITY y las FK `CreatedBy`. |
| ALTER / migraciones EF al arrancar | Convención del proyecto: scripts vírgenes + recrear. |

## Consecuencias

- Toda tabla nueva de Sistema copia este molde (mutable o inmutable).
- Application usa `StatusId == RecordStatusIds.Active` (no `IsActive`).
- AccessLogs resuelve `CreatedBy` desde `IdentityUserId` → `UserProfiles.Id`.
- Ampliar el molde (p. ej. StatusId en RecordStatuses) requiere adenda.

## Adenda (2026-10-14) — Categories y MovementTypes (T02-06)

| Tabla | Notas |
|-------|--------|
| Categories | Mutable; `Name` nvarchar(50) único; seed Electrónica / Papelería / Limpieza; `CreatedBy` NULL. |
| MovementTypes | Mutable; `Code` varchar(10) único (`ENTRADA` / `SALIDA` / `AJUSTE`); `Name` nvarchar(30); seed de esos 3; `CreatedBy` NULL. |
| RecordStatuses | Sin cambios (no se redefine). |

## Adenda (2026-10-14) — Procedimientos de lectura (T02-10 / SP1–SP6)

Listados y reporte vía `usp_*_List` / `usp_Report_Stock` / `usp_Products_Lookup` (scripts 013–020). Totales en `OUTPUT`; existencia desde `vw_ProductStock`; `ORDER BY` con CASE cerrado; búsqueda con parámetros y `fn_EscapeLikePattern` (B6). Escrituras siguen en la API.

## Adenda (2026-10-14) — ICurrentUser y claim profile_id (T02-09 / E5)

| Tema | Decisión |
|------|----------|
| Sesión API | `ICurrentUser` (scoped): Identity `UserId` + `UserProfileId` (claim o lookup de perfil activo). |
| JWT | Claim `profile_id` = `UserProfiles.Id` (int). La Web lo copia a la cookie al establecer sesión. |
| Auditoría | `AuditStamp.ApplyCreate` / `ApplyUpdate` sobre `IMutableAudited` / `IImmutableAudited`; la Web no envía Created*/Updated*. |

## Adenda (2026-10-14) — Products, InventoryMovements, vw_ProductStock (T02-07)

| Pieza | Notas |
|-------|--------|
| Products | Mutable; sin columna de existencia; `Code` UX; CK precio/mínimo ≥ 0. |
| InventoryMovements | Inmutable `bigint`; CKs de stock; índices ProductId+Id DESC INCLUDE StockAfter y CreatedAtUtc DESC. |
| vw_ProductStock | `ProductId` + `Stock` = último `StockAfter` (0 si no hay movimientos). |
| Seed | 5 productos + ENTRADA inicial; `CreatedBy` del perfil si existe, o se completa en DevelopmentAdminSeeder. |
