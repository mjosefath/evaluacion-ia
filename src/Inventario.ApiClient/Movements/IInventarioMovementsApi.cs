using Inventario.Contracts.Common;
using Inventario.Contracts.Movements;

namespace Inventario.ApiClient.Movements;

public interface IInventarioMovementsApi
{
    Task<OperationResultDto<CreateMovementResponse>> CreateAsync(
        CreateMovementRequest request,
        CancellationToken cancellationToken = default);
}
