using Inventario.Contracts.Common;

namespace Inventario.Contracts.Catalogs;

/// <summary>GET /api/categories?search&amp;statusId&amp;page&amp;pageSize&amp;sort&amp;dir</summary>
public sealed class CategoryListQuery : PagedQuery
{
    /// <summary>NULL = todos; por defecto la API puede enviar 1 (activos).</summary>
    public byte? StatusId { get; set; }
}
