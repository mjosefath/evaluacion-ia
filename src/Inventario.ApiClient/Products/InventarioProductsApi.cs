using System.Net.Http.Json;
using Inventario.ApiClient.Auth;
using Inventario.ApiClient.Errors;
using Inventario.Contracts.Common;
using Inventario.Contracts.Products;

namespace Inventario.ApiClient.Products;

public sealed class InventarioProductsApi(HttpClient httpClient) : IInventarioProductsApi
{
    public async Task<PagedResult<ProductListItemDto>> ListAsync(
        ProductListQuery query,
        CancellationToken cancellationToken = default)
    {
        var qs = BuildQuery(query);
        var url = string.IsNullOrEmpty(qs) ? "api/products" : $"api/products?{qs}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        return await ReadSuccessAsync<PagedResult<ProductListItemDto>>(response, cancellationToken);
    }

    public async Task<ProductDetailDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/products/{id}", cancellationToken);
        return await ReadSuccessAsync<ProductDetailDto>(response, cancellationToken);
    }

    public async Task<ProductDetailDto> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/products",
            request,
            InventarioJson.Options,
            cancellationToken);
        return await ReadSuccessAsync<ProductDetailDto>(response, cancellationToken);
    }

    public async Task<ProductDetailDto> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/products/{id}",
            request,
            InventarioJson.Options,
            cancellationToken);
        return await ReadSuccessAsync<ProductDetailDto>(response, cancellationToken);
    }

    private static string BuildQuery(ProductListQuery query)
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

        if (query.StatusId.HasValue)
        {
            parts.Add($"statusId={query.StatusId.Value}");
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
