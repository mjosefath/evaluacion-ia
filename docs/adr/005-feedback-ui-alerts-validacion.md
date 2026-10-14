# ADR-005 — Feedback UI: alerts y validación de campos

## Contexto

Tras adoptar Dub (ADR-003) e iconografía Lucide local (ADR-004), el feedback de la Web quedaba inconsistente: success/info se veían iguales, los errores de login eran un `<p class="form-error">` suelto, y las clases de `jquery.validate.unobtrusive` no tenían estilo de sistema. El dueño pidió un estándar AAA (limpio, minimalista, Dub) antes de seguir con funcionalidad.

## Decisión

| Tema | Decisión |
|------|----------|
| Tokens de tint | App-only en `theme.css`: `--color-danger-soft` (`#fef2f2`), `--color-info-soft` (`#eff6ff`). Success usa `--surface-tinted-accent` / soft-mint. |
| Alerts | Border-first: `.alert-success` (mint + ash), `.alert-info` (info-soft + ash), `.alert-error` (danger-soft + borde/texto danger). Sin sombras pesadas. |
| Markup | `.alert` + icono Lucide (`_AlertIcon`) + `.alert-content`. TempData vía `_Alerts`; Login `ErrorMessage` → `alert alert-error` (no banner ad hoc). |
| Iconos | Subset Lucide local: `circle-check`, `info`, `circle-alert`; `aria-hidden`; color `currentColor` / heredado del alert. |
| Validación unobtrusive | `.form-input.input-validation-error` / `.is-invalid`: borde danger + focus ring suave. `.field-validation-error` / `.form-error`: caption danger. `.field-validation-valid` y `:empty`: ocultos. |
| Summary | `.validation-summary-errors`: caja compacta alineada a alert-error (lista sin viñetas). Usar `class="validation-summary-errors"` en `asp-validation-summary`. |
| Contraste | En `alert-error`, el texto del contenido hereda el color danger (no gris `text-muted` sobre tint). |

## Alternativas descartadas

| Alternativa | Por qué no |
|-------------|------------|
| Toasts / librería de notificaciones | Dependencia y patrón ajeno al shell Dub actual. |
| Verde/rojo chillón a pantalla completa | Choca con Do/Don't Dub (tints suaves, no grandes fills cromáticos). |
| Solo texto rojo sin tint ni icono | Poco escaneable; no cumple el estándar AAA pedido. |
| CDN de iconos o set distinto | Viola libs locales + LICENSE Lucide ya en repo. |
| Mantener ErrorMessage como `.form-error` suelto | Rompe la jerarquía unificada con TempData/Error. |

## Consecuencias

- Nuevos mensajes de página usan `_Alerts` / `alert-*` + `_AlertIcon`; no inventar banners con clases sueltas.
- Formularios (incl. modal AJAX) exponen errores de campo con `asp-validation-for` + `.form-error` y summaries con `.validation-summary-errors`.
- Nuevos tints de estado se añaden como tokens app-only en `theme.css`, no reinventando hex en vistas.
- Cambiar el patrón (p. ej. toasts, otro set de iconos de estado) requiere ADR nuevo o adenda.
