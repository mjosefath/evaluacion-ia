using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Inventario.Contracts.Auth;

namespace Inventario.ApiClient.Auth;

/// <summary>
/// Añade Bearer y, ante 401, renueva con refresh una sola vez (sin loops).
/// El cliente de refresh no usa este handler (nombre InventarioApiClientNames.Refresh).
/// </summary>
public sealed class AuthRefreshHandler(
    IAuthTokenStore tokenStore,
    IHttpClientFactory httpClientFactory) : DelegatingHandler
{
    private static readonly SemaphoreSlim RefreshGate = new(1, 1);
    private static readonly HttpRequestOptionsKey<bool> RetriedKey = new("Inventario.Auth.Retried");

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken);
        }

        await AttachBearerAsync(request, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized
            || ShouldSkipRefresh(request)
            || IsRetried(request))
        {
            return response;
        }

        var accessBefore = await tokenStore.GetAccessTokenAsync(cancellationToken);

        await RefreshGate.WaitAsync(cancellationToken);
        try
        {
            var accessNow = await tokenStore.GetAccessTokenAsync(cancellationToken);

            // Otro request ya renovó mientras esperábamos el lock.
            if (string.IsNullOrEmpty(accessNow)
                || string.Equals(accessNow, accessBefore, StringComparison.Ordinal))
            {
                var refreshed = await TryRefreshAsync(cancellationToken);
                if (!refreshed)
                {
                    await tokenStore.ClearAsync(cancellationToken);
                    return response;
                }
            }
        }
        finally
        {
            RefreshGate.Release();
        }

        response.Dispose();

        var retry = await CloneAsync(request, cancellationToken);
        retry.Options.Set(RetriedKey, true);
        await AttachBearerAsync(retry, cancellationToken);
        return await base.SendAsync(retry, cancellationToken);
    }

    private async Task AttachBearerAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = null;
        var access = await tokenStore.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(access))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
        }
    }

    private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
    {
        var refreshToken = await tokenStore.GetRefreshTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var client = httpClientFactory.CreateClient(InventarioApiClientNames.Refresh);
        using var refreshResponse = await client.PostAsJsonAsync(
            "api/auth/refresh",
            new RefreshRequest(refreshToken),
            InventarioJson.Options,
            cancellationToken);

        if (!refreshResponse.IsSuccessStatusCode)
        {
            return false;
        }

        var tokens = await refreshResponse.Content.ReadFromJsonAsync<AuthTokenResponse>(
            InventarioJson.Options,
            cancellationToken);

        if (tokens is null
            || string.IsNullOrWhiteSpace(tokens.AccessToken)
            || string.IsNullOrWhiteSpace(tokens.RefreshToken))
        {
            return false;
        }

        await tokenStore.SetTokensAsync(tokens, cancellationToken);
        return true;
    }

    private static bool ShouldSkipRefresh(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        return path.EndsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith("/api/auth/refresh", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRetried(HttpRequestMessage request) =>
        request.Options.TryGetValue(RetriedKey, out var retried) && retried;

    private static async Task<HttpRequestMessage> CloneAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = content;
        }

        foreach (var option in request.Options)
        {
            clone.Options.TryAdd(option.Key, option.Value);
        }

        return clone;
    }
}
