using Inventario.Application.Results;
using Inventario.Contracts.Movements;

namespace Inventario.Application.Movements;

public interface IMovementCommandService
{
    Task<Result<CreateMovementResponse>> CreateAsync(
        CreateMovementRequest request,
        CancellationToken cancellationToken = default);
}
