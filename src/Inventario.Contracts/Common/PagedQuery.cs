namespace Inventario.Contracts.Common;

/// <summary>
/// Parámetros comunes de listado (PG2–PG3). Query string: search, page, pageSize, sort, dir.
/// </summary>
public class PagedQuery
{
    public string? Search { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Sort { get; set; }

    /// <summary>asc | desc (insensible a mayúsculas).</summary>
    public string? Dir { get; set; }
}
