using System.Net.Http.Json;
using Inventario.ApiClient.Auth;
using Inventario.ApiClient.Errors;
using Inventario.Contracts.Common;
using Inventario.Contracts.Movements;

namespace Inventario.ApiClient.Movements;

public sealed class InventarioMovementsApi(HttpClient httpClient) : IInventarioMovementsApi
{
    public async Task<OperationResultDto<CreateMovementResponse>> CreateAsync(
        CreateMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/movements",
            request,
            InventarioJson.Options,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ApiError.FromHttpResponseAsync(response, cancellationToken);
            throw new InventarioApiException(error);
        }

        var value = await response.Content.ReadFromJsonAsync<OperationResultDto<CreateMovementResponse>>(
            InventarioJson.Options,
            cancellationToken);

        if (value?.Data is null)
        {
            throw new InventarioApiException(new ApiError
            {
                Status = (int)response.StatusCode,
                Code = "unexpected",
                Detail = "La API devolvió una respuesta vacía o inválida."
            });
        }

        return value;
    }
}
