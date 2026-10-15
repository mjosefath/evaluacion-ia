using Inventario.Application.Results;
using Inventario.Contracts.Catalogs;
using Inventario.Contracts.Common;

namespace Inventario.Application.Catalogs;

public interface ICatalogQueryService
{
    Task<Result<PagedResult<CategoryListItemDto>>> ListCategoriesAsync(
        CategoryListQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<MovementTypeListItemDto>>> ListMovementTypesAsync(
        MovementTypeListQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<RecordStatusListItemDto>>> ListRecordStatusesAsync(
        RecordStatusListQuery query,
        CancellationToken cancellationToken = default);
}
