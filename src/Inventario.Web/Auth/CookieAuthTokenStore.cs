using System.Security.Claims;
using Inventario.ApiClient.Auth;
using Inventario.Contracts.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Inventario.Web.Auth;

/// <summary>
/// Tokens en el ticket de cookie (Data Protection / HttpOnly). Caché en HttpContext.Items para el request actual.
/// </summary>
public sealed class CookieAuthTokenStore(IHttpContextAccessor httpContextAccessor) : IAuthTokenStore
{
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var tokens = await GetCachedOrCookieTokensAsync(cancellationToken);
        return tokens?.AccessToken;
    }

    public async Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        var tokens = await GetCachedOrCookieTokensAsync(cancellationToken);
        return tokens?.RefreshToken;
    }

    public async Task SetTokensAsync(AuthTokenResponse tokens, CancellationToken cancellationToken = default)
    {
        var http = GetHttpContext();
        http.Items[AuthTokenKeys.ItemsCache] = tokens;

        var auth = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = auth.Principal is { Identity.IsAuthenticated: true }
            ? auth.Principal
            : CreatePendingPrincipal();

        var properties = auth.Properties ?? new AuthenticationProperties();
        ApplyTokens(properties, tokens);

        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        var http = GetHttpContext();
        http.Items.Remove(AuthTokenKeys.ItemsCache);
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private async Task<AuthTokenResponse?> GetCachedOrCookieTokensAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var http = GetHttpContext();
        if (http.Items[AuthTokenKeys.ItemsCache] is AuthTokenResponse cached)
        {
            return cached;
        }

        var auth = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!auth.Succeeded || auth.Properties is null)
        {
            return null;
        }

        return ReadTokens(auth.Properties);
    }

    internal static void ApplyTokens(AuthenticationProperties properties, AuthTokenResponse tokens)
    {
        properties.Items[AuthTokenKeys.AccessToken] = tokens.AccessToken;
        properties.Items[AuthTokenKeys.RefreshToken] = tokens.RefreshToken;
        properties.Items[AuthTokenKeys.AccessExpires] = tokens.AccessTokenExpiresAtUtc.ToString("O");
        properties.Items[AuthTokenKeys.RefreshExpires] = tokens.RefreshTokenExpiresAtUtc.ToString("O");
        properties.IsPersistent = true;
        properties.AllowRefresh = true;
        properties.ExpiresUtc = tokens.RefreshTokenExpiresAtUtc;
        properties.IssuedUtc = DateTimeOffset.UtcNow;
    }

    internal static AuthTokenResponse? ReadTokens(AuthenticationProperties properties)
    {
        if (!properties.Items.TryGetValue(AuthTokenKeys.AccessToken, out var access)
            || string.IsNullOrWhiteSpace(access)
            || !properties.Items.TryGetValue(AuthTokenKeys.RefreshToken, out var refresh)
            || string.IsNullOrWhiteSpace(refresh))
        {
            return null;
        }

        var accessExpires = properties.Items.TryGetValue(AuthTokenKeys.AccessExpires, out var ae)
            && DateTimeOffset.TryParse(ae, out var accessExp)
                ? accessExp
                : DateTimeOffset.UtcNow.AddMinutes(15);

        var refreshExpires = properties.Items.TryGetValue(AuthTokenKeys.RefreshExpires, out var re)
            && DateTimeOffset.TryParse(re, out var refreshExp)
                ? refreshExp
                : DateTimeOffset.UtcNow.AddDays(7);

        return new AuthTokenResponse(access, refresh, accessExpires, refreshExpires);
    }

    private static ClaimsPrincipal CreatePendingPrincipal()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "pending")],
            CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private HttpContext GetHttpContext() =>
        httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("HttpContext no disponible para el almacén de tokens.");
}
