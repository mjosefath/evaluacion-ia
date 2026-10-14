# ADR-008 — Result general, ApiProblem y catálogo E2 (borrador)

## Contexto

El análisis 03 unifica errores/avisos/validaciones. ADR-002 limitaba el patrón a auth (`AuthResult` / `AuthErrorCode`). Hay que sentar el contrato único (E1–E3) antes de alinear mappers (T03-06/07) y productos (02).

## Decisión

| Tema | Decisión |
|------|----------|
| Resultado de casos de uso | `Result` / `Result<T>` en Application (`Code`, `Message`, `FieldErrors`, `Rule`, `Warnings` W10). |
| Códigos | Constantes string en `ErrorCodes` / `BusinessRuleCodes` (E2); no enums como contrato. |
| Respuesta HTTP de error | `ApiProblem` en Contracts (E3): type, title ES, status, detail, code, rule?, errors?, traceId. |
| Auth legacy | `AuthResult<T>` envuelve `Result<T>`; `AuthErrorCode` mapea vía `AuthErrorCodeMapper` hasta T03-06/07. |
| Mensajes | `MessageCatalog` en Application (M2); la Web no reescribe. |
| ValidationProblemDetailsDto | Obsoleto; fuente de verdad = `ApiProblem`. |

## Alternativas descartadas

| Alternativa | Por qué no |
|-------------|------------|
| Mantener solo AuthResult | No sirve para productos/movimientos (A1). |
| Enum serializado como contrato | Frágil ante renombres/orden (decisión 03). |
| Excepciones de negocio | Ya descartado en ADR-002; se reafirma. |

## Consecuencias

- Casos de uso nuevos devuelven `Result`/`Result<T>` con códigos E2.
- El mapeador único Result→HTTP (E5) y el traductor Web (W1) se completan en tareas siguientes.
- Adenda conceptual a ADR-002: auth converge al Result general.
