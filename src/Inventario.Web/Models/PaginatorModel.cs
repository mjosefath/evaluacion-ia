namespace Inventario.Web.Models;

/// <summary>Paginador de listados (PG2 / PG4): rango, páginas, pageSize 20/50/100.</summary>
public sealed class PaginatorModel
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public int TotalRows { get; init; }

    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRows / (double)PageSize);

    public int FromRow =>
        TotalRows == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    public int ToRow =>
        TotalRows == 0 ? 0 : Math.Min(Page * PageSize, TotalRows);

    public string PageParam { get; init; } = "page";

    public string PageSizeParam { get; init; } = "pageSize";
}
