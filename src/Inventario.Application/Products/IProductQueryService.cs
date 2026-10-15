using Inventario.Application.Results;
using Inventario.Contracts.Common;
using Inventario.Contracts.Products;

namespace Inventario.Application.Products;

public interface IProductQueryService
{
    Task<Result<PagedResult<ProductListItemDto>>> ListAsync(
        ProductListQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ProductDetailDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);
}
