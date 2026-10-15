using System.Globalization;
using System.Net.Http.Json;
using Inventario.ApiClient.Auth;
using Inventario.ApiClient.Errors;
using Inventario.Contracts.Common;
using Inventario.Contracts.Movements;

namespace Inventario.ApiClient.Movements;

public sealed class InventarioMovementsApi(HttpClient httpClient) : IInventarioMovementsApi
{
    public async Task<PagedResult<MovementListItemDto>> ListAsync(
        MovementListQuery query,
        CancellationToken cancellationToken = default)
    {
        var qs = BuildListQuery(query);
        var url = string.IsNullOrEmpty(qs) ? "api/movements" : $"api/movements?{qs}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        return await ReadSuccessAsync<PagedResult<MovementListItemDto>>(response, cancellationToken);
    }

    public async Task<OperationResultDto<CreateMovementResponse>> CreateAsync(
        CreateMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/movements",
            request,
            InventarioJson.Options,
            cancellationToken);

        return await ReadSuccessAsync<OperationResultDto<CreateMovementResponse>>(response, cancellationToken);
    }

    private static string BuildListQuery(MovementListQuery query)
    {
        var parts = new List<string>
        {
            $"page={query.Page}",
            $"pageSize={query.PageSize}"
        };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            parts.Add($"search={Uri.EscapeDataString(query.Search.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(query.Sort))
        {
            parts.Add($"sort={Uri.EscapeDataString(query.Sort.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(query.Dir))
        {
            parts.Add($"dir={Uri.EscapeDataString(query.Dir.Trim())}");
        }

        if (query.TypeId.HasValue)
        {
            parts.Add($"typeId={query.TypeId.Value}");
        }

        if (query.From.HasValue)
        {
            parts.Add($"from={Uri.EscapeDataString(query.From.Value.UtcDateTime.ToString("o", CultureInfo.InvariantCulture))}");
        }

        if (query.To.HasValue)
        {
            parts.Add($"to={Uri.EscapeDataString(query.To.Value.UtcDateTime.ToString("o", CultureInfo.InvariantCulture))}");
        }

        return string.Join('&', parts);
    }

    private static async Task<T> ReadSuccessAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await ApiError.FromHttpResponseAsync(response, cancellationToken);
            throw new InventarioApiException(error);
        }

        var value = await response.Content.ReadFromJsonAsync<T>(InventarioJson.Options, cancellationToken);
        if (value is null)
        {
            throw new InventarioApiException(new ApiError
            {
                Status = (int)response.StatusCode,
                Code = "unexpected",
                Detail = "La API devolvió una respuesta vacía o inválida."
            });
        }

        if (value is OperationResultDto<CreateMovementResponse> op && op.Data is null)
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
