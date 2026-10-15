using Inventario.Contracts.Common;

namespace Inventario.Contracts.Products;

/// <summary>GET /api/products?search&amp;categoryId&amp;statusId&amp;page&amp;pageSize&amp;sort&amp;dir</summary>
public sealed class ProductListQuery : PagedQuery
{
    public int? CategoryId { get; set; }

    /// <summary>NULL = todos; UI por defecto Activo (1).</summary>
    public byte? StatusId { get; set; }
}
