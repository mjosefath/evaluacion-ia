using System.Net.Http.Json;
using Inventario.ApiClient.Auth;
using Inventario.ApiClient.Errors;
using Inventario.Contracts.Catalogs;
using Inventario.Contracts.Common;

namespace Inventario.ApiClient.Catalogs;

public sealed class InventarioCatalogsApi(HttpClient httpClient) : IInventarioCatalogsApi
{
    public Task<PagedResult<CategoryListItemDto>> ListCategoriesAsync(
        CategoryListQuery query,
        CancellationToken cancellationToken = default) =>
        GetPagedAsync<CategoryListItemDto>("api/categories", BuildCatalogQuery(query, query.StatusId), cancellationToken);

    public Task<PagedResult<MovementTypeListItemDto>> ListMovementTypesAsync(
        MovementTypeListQuery query,
        CancellationToken cancellationToken = default) =>
        GetPagedAsync<MovementTypeListItemDto>(
            "api/movement-types",
            BuildCatalogQuery(query, query.StatusId),
            cancellationToken);

    public Task<PagedResult<RecordStatusListItemDto>> ListRecordStatusesAsync(
        RecordStatusListQuery query,
        CancellationToken cancellationToken = default) =>
        GetPagedAsync<RecordStatusListItemDto>("api/record-statuses", BuildPagedQuery(query), cancellationToken);

    private async Task<PagedResult<T>> GetPagedAsync<T>(
        string path,
        string queryString,
        CancellationToken cancellationToken)
    {
        var url = string.IsNullOrEmpty(queryString) ? path : $"{path}?{queryString}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var value = await response.Content.ReadFromJsonAsync<PagedResult<T>>(
            InventarioJson.Options,
            cancellationToken);

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

    private static string BuildCatalogQuery(PagedQuery query, byte? statusId)
    {
        var parts = BuildPagedParts(query);
        if (statusId.HasValue)
        {
            parts.Add($"statusId={statusId.Value}");
        }

        return string.Join('&', parts);
    }

    private static string BuildPagedQuery(PagedQuery query) =>
        string.Join('&', BuildPagedParts(query));

    private static List<string> BuildPagedParts(PagedQuery query)
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

        return parts;
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await ApiError.FromHttpResponseAsync(response, cancellationToken);
        throw new InventarioApiException(error);
    }
}
