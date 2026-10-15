# ADR-002 — Resultado de casos de uso de autenticación

## Contexto

La Api debe mapear fallos de auth a 401/403/423/400 sin tratar reglas de negocio como excepciones no controladas. Application necesita devolver éxito + DTO o un código de fallo estable.

## Decisión

Usar `AuthResult<T>` con `AuthErrorCode` (InvalidCredentials, Lockout, InactiveProfile, Validation, Unauthorized, InvalidRefreshToken, PasswordChangeFailed, NotFound).

- Éxito: `IsSuccess` + `Value` (DTO de Contracts).
- Fallo: `ErrorCode` + mensaje genérico opcional + `FieldErrors` solo en Validation.
- La Api traduce códigos a HTTP; no se filtran excepciones de negocio hacia el cliente.

## Alternativas descartadas

| Alternativa | Por qué no |
|-------------|------------|
| Excepciones por cada fallo | Contamina el host y dificulta el mapeo uniforme a ProblemDetails. |
| Result genérico de librería externa | Dependencia innecesaria para un solo módulo. |
| Códigos HTTP dentro de Application | Acopla Application al transporte. |

## Consecuencias

- Controladores delgados: invocan `IAuthService` y mapean `AuthErrorCode`.
- Nuevos fallos de auth añaden valor al enum y al mapeo en Api.

## Adenda (2026-10-14) — Convergencia a Result general (análisis 03 / ADR-008)

`AuthResult` / `AuthErrorCode` / `AuthResultHttpMapper` fueron eliminados. Auth usa el mismo `Result` / `Result<T>` y `ErrorCodes` que el resto de Application (p. ej. `auth.invalid_credentials`, `auth.locked`, `auth.inactive_profile`, `auth.session_expired`). El mapeo HTTP y el contrato de error están en ADR-008 (`ResultHttpMapper`, `ApiProblem`). La decisión de no usar excepciones de negocio se mantiene.
