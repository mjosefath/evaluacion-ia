using Inventario.Contracts.Catalogs;
using Inventario.Contracts.Common;

namespace Inventario.ApiClient.Catalogs;

public interface IInventarioCatalogsApi
{
    Task<PagedResult<CategoryListItemDto>> ListCategoriesAsync(
        CategoryListQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedResult<MovementTypeListItemDto>> ListMovementTypesAsync(
        MovementTypeListQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedResult<RecordStatusListItemDto>> ListRecordStatusesAsync(
        RecordStatusListQuery query,
        CancellationToken cancellationToken = default);
}
