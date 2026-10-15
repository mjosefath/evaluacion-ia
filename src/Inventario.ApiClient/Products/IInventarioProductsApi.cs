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

    Task<ProductDetailDto> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductDetailDto> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductDetailDto> DeactivateAsync(
        int id,
        ProductStatusChangeRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductDetailDto> ActivateAsync(
        int id,
        ProductStatusChangeRequest request,
        CancellationToken cancellationToken = default);
}
