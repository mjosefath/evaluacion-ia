using Inventario.Application.Results;
using Inventario.Contracts.Products;

namespace Inventario.Application.Products;

public interface IProductCommandService
{
    Task<Result<ProductDetailDto>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ProductDetailDto>> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default);
}
