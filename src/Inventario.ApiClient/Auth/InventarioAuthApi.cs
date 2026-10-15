using System.Net.Http.Json;
using Inventario.ApiClient.Errors;
using Inventario.Contracts.Auth;

namespace Inventario.ApiClient.Auth;

public sealed class InventarioAuthApi(HttpClient httpClient, IAuthTokenStore tokenStore) : IInventarioAuthApi
{
    private const string LoginPath = "api/auth/login";
    private const string RefreshPath = "api/auth/refresh";
    private const string LogoutPath = "api/auth/logout";
    private const string ChangePasswordPath = "api/auth/change-password";
    private const string MePath = "api/auth/me";

    public async Task<AuthTokenResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            LoginPath,
            request,
            InventarioJson.Options,
            cancellationToken);

        var tokens = await ReadSuccessAsync<AuthTokenResponse>(response, cancellationToken);
        await tokenStore.SetTokensAsync(tokens, cancellationToken);
        return tokens;
    }

    public async Task<AuthTokenResponse> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            RefreshPath,
            request,
            InventarioJson.Options,
            cancellationToken);

        var tokens = await ReadSuccessAsync<AuthTokenResponse>(response, cancellationToken);
        await tokenStore.SetTokensAsync(tokens, cancellationToken);
        return tokens;
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            LogoutPath,
            request,
            InventarioJson.Options,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        await tokenStore.ClearAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            ChangePasswordPath,
            request,
            InventarioJson.Options,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        await tokenStore.ClearAsync(cancellationToken);
    }

    public async Task<MeResponse> MeAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(MePath, cancellationToken);
        return await ReadSuccessAsync<MeResponse>(response, cancellationToken);
    }

    private static async Task<T> ReadSuccessAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
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

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // No se registra el cuerpo: la Web usa ApiError tipado.
        var error = await ApiError.FromHttpResponseAsync(response, cancellationToken);
        throw new InventarioApiException(error);
    }
}
