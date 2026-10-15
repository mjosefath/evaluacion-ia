using Inventario.Contracts.Common;
using Inventario.Contracts.Products;

namespace Inventario.ApiClient.Products;

public interface IInventarioProductsApi
{
    Task<PagedResult<ProductListItemDto>> ListAsync(
        ProductListQuery query,
        CancellationToken cancellationToken = default);

    Task<ProductDetailDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);
}
