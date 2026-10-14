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
