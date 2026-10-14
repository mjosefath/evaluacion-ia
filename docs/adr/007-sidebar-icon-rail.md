# ADR-007 — Sidebar icon-rail (desktop) vs drawer (narrow)

## Contexto

ADR-006 fijó el drawer off-canvas solo en viewport estrecho (≤800px) y el dark mode persistente. El dueño pidió además que, en desktop, la hamburguesa colapse el sidebar a un rail de iconos (sin textos), con preferencia persistente — sin sustituir el drawer móvil ni mezclar ambos estados.

## Decisión

| Tema | Decisión |
|------|----------|
| Desktop (≥801px) | Toggle → `html.nav-collapsed`: sidebar ~`--sidebar-width-collapsed` (56px), marca “I”, labels ocultas (accesibles vía `title` / `aria-label`). |
| Narrow (≤800px) | Solo drawer: `body.nav-open` + overlay; **no** icon-rail. Al entrar en narrow se quita `nav-collapsed`. |
| Persistencia | `localStorage["inventario.navCollapsed"]` = `"1"` \| `"0"`. Solo aplica en desktop. |
| Anti-flash | `_NavBoot` en `<head>` del layout auth: si ≥801px y storage `"1"`, añade `html.nav-collapsed` antes del CSS. |
| APIs JS | Drawer: `openNav` / `closeNav` / `toggleNav`. Icon-rail: `setNavCollapsed` / `toggleNavCollapsed`. El mismo `#app-nav-toggle` despacha según `matchMedia`. |
| Iconos / aria | Panel “expandido” (drawer abierto o sidebar ancho) → icono X, `aria-expanded=true`. Panel “compacto” (drawer cerrado o icon-rail) → menu, `aria-expanded=false`. Labels distintos: abrir/cerrar navegación vs colapsar/expandir menú lateral. |
| Resize | narrow→desktop: cierra drawer y reaplica collapsed desde storage. desktop→narrow: quita `nav-collapsed`. |

## Alternativas descartadas

| Alternativa | Por qué no |
|-------------|------------|
| Un solo estado (`nav-open`) para colapso y drawer | Comportamientos y anchos distintos; mezclar rompe el layout. |
| Icon-rail también en móvil | Choca con P1-A (drawer off-canvas en narrow). |
| Colapsar ocultando todo el sidebar en desktop | El dueño pidió rail de iconos, no ocultación total. |
| Persistir el drawer abierto | Innecesario; ADR-006 ya lo descartó. |

## Consecuencias

- Nuevos ítems del sidebar deben llevar icono + `.app-nav-label` (y `title`/`aria-label`) para el rail.
- Cambiar el breakpoint o unificar drawer/rail requiere ADR nuevo o adenda.
- La clave `inventario.navCollapsed` es contrato de cliente; renombrarla implica migración o pérdida de preferencia.
- `_NavBoot` solo en layout autenticado (Login no tiene sidebar).
