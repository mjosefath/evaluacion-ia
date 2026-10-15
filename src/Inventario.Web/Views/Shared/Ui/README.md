# UI compartida (T02-13)

Partials y JS para listados (análisis 02 / PG7–PG10, FL1–FL6).

## Partials

| Archivo | Uso |
|---------|-----|
| `_FilterBar.cshtml` | Búsqueda + slot de filtros + Limpiar + badges |
| `_Paginator.cshtml` | Mostrando X–Y de Z, prev/next, páginas, pageSize |
| `_CatalogLookup.cshtml` | Tom Select → `/catalogs/{categories\|movement-types\|record-statuses}` |
| `_DemoCatalogFilters.cshtml` | Slots de ejemplo (demo) |

## JS / assets

- `wwwroot/js/inventario-filters.js` — `InventarioUi.filters.bind(root)`, `InventarioUi.urlQuery`
- `wwwroot/lib/tom-select/` — Tom Select **2.6.2** Apache-2.0 (ver README allí)

## Cableado (T02-14+)

```cshtml
<div data-list-page data-list-navigate="reload">
  @{
    var filters = await Html.PartialAsync("…filtros de pantalla…");
  }
  <partial name="Ui/_FilterBar" model="new FilterBarModel { Search = …, Filters = filters }" />
  @* tabla *@
  <partial name="Ui/_Paginator" model="new PaginatorModel { Page, PageSize, TotalRows }" />
</div>
@section Scripts {
  <script>
    document.addEventListener("DOMContentLoaded", () => {
      const root = document.querySelector("[data-list-page]");
      window.InventarioUi?.filters?.bind(root);
    });
  </script>
}
```

- Filtros: `activeOnly=false` (todos, FL6). Captura: `ActiveOnly=true` (solo activos).
- Proxy Web: `GET /catalogs/categories|movement-types|record-statuses` (cookie → API Bearer).

## Demo

`GET /ui-demo/filters` (autenticado).
