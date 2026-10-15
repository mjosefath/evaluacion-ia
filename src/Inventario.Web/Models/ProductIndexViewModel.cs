using Inventario.Contracts.Common;
using Inventario.Contracts.Products;

namespace Inventario.Web.Models;

public sealed class ProductIndexViewModel
{
    public required ProductListQuery Query { get; init; }

    public required PagedResult<ProductListItemDto> Result { get; init; }

    public string? CategoryLabel { get; init; }

    public string? StatusLabel { get; init; }

    /// <summary>True si statusId venía vacío en la query (todos).</summary>
    public bool StatusFilterAll { get; init; }
}
