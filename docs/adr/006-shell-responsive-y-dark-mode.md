# ADR-006 — Shell responsive (hamburguesa/drawer) y dark mode persistente

## Contexto

El shell autenticado tenía sidebar siempre en flujo (también en móvil, apilado). El dueño pidió P1-A: en viewport estrecho el menú es drawer off-canvas con hamburguesa; en desktop el sidebar permanece fijo sin colapso. En paralelo, P2-A: dark mode con tokens Dub, toggle en UI y preferencia persistente (default light), sin flash al recargar.

## Decisión

| Tema | Decisión |
|------|----------|
| Breakpoint | `max-width: 800px` = narrow; `≥801px` = desktop. |
| Desktop | Grid `sidebar + main` como hasta ahora; botón hamburguesa y overlay ocultos; **sin** colapso del sidebar. |
| Narrow | Sidebar `position: fixed` off-canvas (`translateX(-100%)`); main a ancho completo; hamburguesa visible. |
| Abrir/cerrar | Clase `body.nav-open`; overlay clicable; Esc (si no hay dialog abierto); click en `.app-nav-link`. API: `InventarioUi.openNav` / `closeNav` / `toggleNav`. |
| a11y nav | `aria-controls="app-sidebar"`, `aria-expanded` en el toggle; iconos Lucide `menu` / `x`. |
| Persistencia nav | **No** guardar abierto/cerrado en `localStorage` (solo sesión de viewport). |
| Dark tokens | `html[data-theme="dark"]` en `theme.css` redefine tokens semánticos (surfaces, texto, ash, tints, CTA invertido, overlays). Light = `:root` / `data-theme="light"`. |
| Preferencia tema | Clave `localStorage["inventario.theme"]` = `"light"` \| `"dark"`. Default **light** si no hay valor (no `prefers-color-scheme` en primera visita). |
| Anti-flash | Script mínimo `_ThemeBoot` en `<head>` de `_Layout` y `_LoginLayout` antes de CSS. |
| Toggle | `_ThemeToggle` en header autenticado y Login; `InventarioUi.setTheme` / `toggleTheme` / `getTheme`; iconos `moon` / `sun`. |

## Alternativas descartadas

| Alternativa | Por qué no |
|-------------|------------|
| Colapsar sidebar también en desktop | Choca con P1-A (desktop siempre visible). |
| Persistir estado abierto del drawer | Innecesario; al ampliar viewport se cierra; P2-A limita persistencia al tema. |
| Default dark o seguir `prefers-color-scheme` | P2-A fija default light en primera visita. |
| Dark solo con clases en `body` / hardcodes en `site.css` | Rompe el modelo de tokens semánticos de ADR-003/005. |
| Librería de theme/drawer o CDN de iconos | Viola libs locales y el shell nativo actual. |

## Consecuencias

- Nuevos ítems de navegación siguen siendo `.app-nav-link` para cerrar el drawer en narrow al navegar.
- Cambios de color en dark/light pasan por tokens en `theme.css`, no hex sueltos en componentes.
- Ampliar el breakpoint o añadir colapso desktop requiere ADR nuevo o adenda.
- La clave `inventario.theme` es contrato de cliente; renombrarla implica migración o pérdida de preferencia.
