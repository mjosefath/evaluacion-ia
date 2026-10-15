# ADR-004 — Modales de acción, Lucide local e ítem activo del menú

## Contexto

Inventario.Web necesita formularios de acción (p. ej. cambiar contraseña) sin salir de la página actual, iconografía en menús con licencia compatible, y un estado visual claro del ítem activo del sidebar. El dueño pidió modal de acción reutilizable, ChangePassword solo en modal (P1-A), Lucide MIT en local, y activo del menú por ruta (P2-A), no por `localStorage`.

## Decisión

| Tema | Decisión |
|------|----------|
| Modal de acción | `<dialog>` nativo (`.action-modal`), API `InventarioUi.openModal` / `closeModal`. Cierre: Esc y Cancelar; backdrop **no** cierra (evita perder datos del formulario). |
| Formularios en modal | POST por fetch (`data-modal-ajax-form`): errores → partial HTML reinyectado; éxito → JSON `{ redirectUrl }` (p. ej. SignOut + Login). Antiforgery y `[Authorize]` se mantienen. |
| Confirmación | `_Confirm.cshtml` sigue siendo el diálogo de confirmación simple; estilos base compartidos (`.ui-dialog`). |
| Iconos | Subset Lucide en `wwwroot/lib/lucide` (+ `LICENSE`); sprite en `_LucideSprite.cshtml`; `currentColor`; decorativos con `aria-hidden`. Sin CDN. |
| Ítem activo del sidebar | Solo por ruta actual (`controller` / `action`) vía `NavActive.IsMatch`; `aria-current="page"` en el activo. Abrir el modal de ChangePassword **no** cambia la ruta ni el activo. Sin persistir colapso/activo en `localStorage`. |

## Alternativas descartadas

| Alternativa | Por qué no |
|-------------|------------|
| Bootstrap Modal / librería de modales | Dependencia y markup ajenos al patrón nativo ya usado en confirm. |
| Full post + TempData para reabrir el modal tras 400 | Complica el ciclo de vida; el partial AJAX mantiene errores en el modal sin recargar la página. |
| Iconos por CDN o otro set sin licencia en repo | Viola la regla de libs locales y licencias MIT/Apache/BSD. |
| Activo del menú en `localStorage` o “última pantalla visitada” | Choca con P2-A; el modal no debe fingir navegación. |
| Página `/Account/ChangePassword` además del modal | Duplica UX; el GET redirige a Inicio. |

## Consecuencias

- Nuevas acciones con formulario en página usan el shell `_ActionModal` + partial + `data-modal-ajax-form` (o convención equivalente), no rutas de página dedicadas salvo que un ADR lo justifique.
- Nuevos iconos se añaden al subset local y al sprite; no se introducen CDNs.
- Nuevos ítems del sidebar usan `NavActive.IsMatch(ViewContext, controller, action)` (o solo `controller` si aplica) y mantienen texto visible + icono decorativo.
- Cambiar el criterio de activo (p. ej. por área/módulo) o el patrón de modal requiere ADR nuevo o adenda.

## Adenda (2026-10-14) — AJAX, ApiError y 401≠403 (análisis 03)

| Tema | Decisión |
|------|----------|
| Respuesta AJAX de éxito (W5) | Además de `{ redirectUrl }`: opcional `notify: { type, message }` y `refresh: true` (consume `InventarioUi.notify`; ver ADR-005). |
| Confirm | Configurable vía `InventarioUi.confirm` / `data-confirm-*` (detalle en ADR-005); ya no es solo “cerrar el diálogo”. |
| ApiClient | Fallos tipados como `ApiError` en `InventarioApiException`. `IsUnauthorized` = solo 401; `IsForbidden` = solo 403. El refresh silencioso no trata 403 como sesión vencida. |
| Estándar de errores | Contrato completo en [ADR-008](008-result-apiproblem.md). |
