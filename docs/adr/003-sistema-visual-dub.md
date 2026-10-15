# ADR-003 — Sistema visual Dub

## Contexto

Inventario.Web usaba un tema anterior (inspirado en shadcn/Geist, con acento ember). El dueño aprobó como fuente de verdad el sistema visual Dub en `recursos/ui` (DESIGN.md + theme.css): canvas claro, bordes ash 1px, tipografía densa y un acento electric-blue. Hay que fijar qué se adopta tal cual y qué se adapta a la app MVC (sin landing, sin fuentes nuevas no licenciadas en el repo).

## Decisión

Adoptar Dub en `Inventario.Web` vía tokens en `wwwroot/css/theme.css` y componentes en `site.css`, consumiendo `recursos/ui` sin modificarlo salvo acuerdo del dueño.

| Tema | Decisión |
|------|----------|
| Paleta / spacing / radii / shadows | Tokens Dub (`canvas-white`, `ash`, `charcoal`, `electric-blue`, etc.). |
| Tipografía | Solo Inter local (`wwwroot/lib/inter`). Sin Satoshi ni Geist Mono. Display usa Inter weight 500 como sustituto. |
| Primary filled | Fondo `#000000` (`--color-primary-action-fill`), texto blanco, radius 8px en formularios app. |
| Inputs | Borde near-black (`midnight-ink`), radius 6px. |
| Cards / shell | Blanco, borde ash 1px, radius 12px; elevación border-first (sin sombra pesada en cards). |
| Fondo app | Canvas blanco; `paper-mist` solo en alt/sidebar. |
| App-only | `--color-danger: #e7000b` (errores); `--color-nav-active: #dbeaff` (sidebar activo). |
| Layout | `--page-max-width: 1200px`; sidebar ~240px. |

Fuente de verdad: `recursos/ui`. Desviaciones de app (danger, nav-active, Inter-only) quedan documentadas aquí.

## Alternativas descartadas

| Alternativa | Por qué no |
|-------------|------------|
| Mantener tema shadcn/Geist/ember | Choca con la aprobación del dueño de Dub. |
| Cargar Satoshi / Geist Mono (CDN o archivos nuevos) | Licencias/archivos no autorizados en esta fase; DESIGN permite Inter como sustituto de Satoshi. |
| Usar Deep Sapphire o electric-blue como CTA filled principal | Dub reserva fill negro / sapphire con parsimonia; para formularios app se fijó primary fill `#000`. |
| Paper-mist como fondo de página | Dub define canvas blanco como base; mist es superficie alt. |

## Consecuencias

- Cambios visuales parten de `recursos/ui` y se reflejan en `theme.css` / `site.css`; no reinventar paleta en vistas.
- Danger y nav-active son tokens de app; no están en el theme del recurso y no deben usarse como acentos decorativos.
- Nuevas pantallas reutilizan clases del shell (`.app-*`, `.btn-*`, `.card`, `.form-*`) o amplían `site.css` con los mismos tokens.
- Sustituir Dub o reintroducir otra tipografía de display requiere ADR nuevo o adenda.
