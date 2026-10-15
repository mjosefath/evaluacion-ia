# ADR-008 — Result, ApiProblem y estándar de errores (análisis 03)

**Estado:** Aceptado (2026-10-14)

## Contexto

El análisis 03 unifica errores, avisos y validaciones en todo el sistema. ADR-002 limitaba el patrón a auth (`AuthResult` / `AuthErrorCode`). Tras T03-05…11 el contrato único está implementado en Application, API, ApiClient y Web; este ADR lo fija para módulos futuros (p. ej. productos de `02`).

## Decisión

| Capa | Pieza | Decisión |
|------|-------|----------|
| Application | `Result` / `Result<T>` | Éxito o fallo con `Code`, `Message`, `FieldErrors`, `Rule`; `Warnings` (W10) en éxito. Sin excepciones de negocio. |
| Application | `ErrorCodes` / `BusinessRuleCodes` | Códigos string estables (E2); no enums como contrato. |
| Application | `MessageCatalog` | Mensajes de servidor en español (M1–M2). |
| Contracts | `ApiProblem` | Contrato HTTP de error (E3): type, title ES, status, detail, code, rule?, errors?, traceId. Reemplaza `ValidationProblemDetailsDto` (obsoleto). |
| Api | `ResultHttpMapper` + `ApiProblemFactory` | Mapeo único Result→HTTP (E5); tabla E2 (400/401/403/404/409/423/500); titles ES; `traceId`. |
| Api | 500 / ModelState | `ApiProblemExceptionHandler` → `unexpected` sin stack/SQL (E7); ModelState inválido → `code=validation`. |
| ApiClient | `ApiError` + `InventarioApiException` | Parsea `ApiProblem`; `IsUnauthorized` solo 401; `IsForbidden` solo 403 (A5). Refresh silencioso solo ante 401. |
| Web | `ApiErrorTranslator` + `WebUiMessages` | Traductor W1 (formulario vs página); M3 solo sin conexión / timeout / inesperado local. |
| Web | Páginas / cookie | `AccessDeniedPath` → `/Account/AccessDenied`; NotFound y Error con código de referencia (`traceId`). |
| Web | Avisos / confirm | `FlashMessages` + `InventarioUi.notify` / `confirm` (detalle UI en ADR-005). |
| Contracts / Web | ChangePassword | DataAnnotations y mensajes en `ChangePasswordRequest` / `AuthValidationMessages` (V2); ViewModel hereda el DTO. |

## Alternativas descartadas

| Alternativa | Por qué no |
|-------------|------------|
| Mantener solo `AuthResult` | No sirve para productos/movimientos (A1). |
| Enum serializado como contrato | Frágil ante renombres/orden (decisión 03). |
| Excepciones de negocio | Ya descartado en ADR-002; se reafirma. |
| 403 → Login | Mezcla “sin permiso” con “sin sesión” (A5/A6). |
| La Web reescribe mensajes de la API | Viola M2; duplica el catálogo (A4). |

## Consecuencias

- Casos de uso nuevos devuelven `Result`/`Result<T>` con códigos E2; los controladores de API usan `ResultHttpMapper`.
- Errores HTTP salen como `ApiProblem`; el ApiClient expone `ApiError`; la Web decide UI solo vía `ApiErrorTranslator` (y TempData/`notify` según W1/W3).
- Validaciones de formato se declaran en DTOs de Contracts (V2); Application no duplica esos mensajes.
- Adendas: ADR-002 (auth → Result), ADR-004 (AJAX/sesión), ADR-005 (warning/notify/confirm).
- Ampliar el catálogo E2 o el shape de `ApiProblem` requiere adenda a este ADR.
