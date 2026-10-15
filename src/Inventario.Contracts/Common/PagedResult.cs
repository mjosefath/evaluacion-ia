namespace Inventario.Contracts.Common;

/// <summary>
/// Respuesta paginada estándar (análisis 02 / PG4).
/// </summary>
public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalRows { get; init; }

    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRows / (double)PageSize);
}

/// <summary>
/// Listado paginado con total monetario sobre el filtro completo (reporte / PG4–PG5).
/// </summary>
public sealed class ReportPagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalRows { get; init; }

    public decimal GrandTotal { get; init; }

    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRows / (double)PageSize);
}
