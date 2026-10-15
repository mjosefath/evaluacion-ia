using System.Net.Http.Json;
using Inventario.ApiClient.Auth;
using Inventario.ApiClient.Errors;
using Inventario.Contracts.Common;
using Inventario.Contracts.Reports;

namespace Inventario.ApiClient.Reports;

public sealed class InventarioReportsApi(HttpClient httpClient) : IInventarioReportsApi
{
    public async Task<ReportPagedResult<StockReportItemDto>> ListStockAsync(
        StockReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var qs = BuildQuery(query);
        var url = string.IsNullOrEmpty(qs) ? "api/reports/stock" : $"api/reports/stock?{qs}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        return await ReadSuccessAsync<ReportPagedResult<StockReportItemDto>>(response, cancellationToken);
    }

    private static string BuildQuery(StockReportQuery query)
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

        if (query.CategoryId.HasValue)
        {
            parts.Add($"categoryId={query.CategoryId.Value}");
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

        return value;
    }
}
