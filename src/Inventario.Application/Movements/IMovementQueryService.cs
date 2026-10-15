using Inventario.Application.Results;
using Inventario.Contracts.Common;
using Inventario.Contracts.Movements;

namespace Inventario.Application.Movements;

public interface IMovementQueryService
{
    Task<Result<PagedResult<MovementListItemDto>>> ListAsync(
        MovementListQuery query,
        CancellationToken cancellationToken = default);
}
