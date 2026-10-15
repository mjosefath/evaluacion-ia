using Microsoft.AspNetCore.Html;

namespace Inventario.Web.Models;

/// <summary>Barra de filtros FL1–FL4: búsqueda + slots + Limpiar + badges.</summary>
public sealed class FilterBarModel
{
    public string? Search { get; init; }

    public string SearchPlaceholder { get; init; } = "Buscar…";

    public string SearchParam { get; init; } = "search";

    /// <summary>Filtros específicos de la pantalla (selectores, fechas, etc.).</summary>
    public IHtmlContent? Filters { get; init; }

    /// <summary>Si true, el form hace GET al cambiar; si false, solo JS (data-filter-bar).</summary>
    public bool AutoSubmit { get; init; } = true;
}
