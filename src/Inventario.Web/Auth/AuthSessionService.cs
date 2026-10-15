using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Inventario.Contracts.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Inventario.Web.Auth;

public sealed class AuthSessionService(IHttpContextAccessor httpContextAccessor)
{
    public async Task EstablishAsync(
        MeResponse me,
        AuthTokenResponse tokens,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var http = httpContextAccessor.HttpContext
                   ?? throw new InvalidOperationException("HttpContext no disponible.");

        http.Items[AuthTokenKeys.ItemsCache] = tokens;

        var claims = BuildClaims(me, tokens.AccessToken);
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties();
        CookieAuthTokenStore.ApplyTokens(properties, tokens);

        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var http = httpContextAccessor.HttpContext
                   ?? throw new InvalidOperationException("HttpContext no disponible.");

        http.Items.Remove(AuthTokenKeys.ItemsCache);
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static List<Claim> BuildClaims(MeResponse me, string accessToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, me.DisplayName)
        };

        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var sub = jwt.Subject
                      ?? jwt.Claims.FirstOrDefault(c =>
                          c.Type is JwtRegisteredClaimNames.Sub or ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(sub))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));
            }

            var uniqueName = jwt.Claims.FirstOrDefault(c =>
                c.Type is JwtRegisteredClaimNames.UniqueName or ClaimTypes.Name)?.Value;
            if (!string.IsNullOrWhiteSpace(uniqueName))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, uniqueName));
            }

            // Mismo valor que AuthClaimTypes.UserProfileId en Application (sin ref. a Application).
            var profileId = jwt.Claims.FirstOrDefault(c =>
                c.Type == AuthTokenKeys.UserProfileIdClaim)?.Value;
            if (!string.IsNullOrWhiteSpace(profileId))
            {
                claims.Add(new Claim(AuthTokenKeys.UserProfileIdClaim, profileId));
            }
        }
        catch (ArgumentException)
        {
            // Token ilegible: la sesión sigue con DisplayName/roles/permisos de /me.
        }

        foreach (var role in me.Roles.Distinct(StringComparer.Ordinal))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in me.Permissions.Distinct(StringComparer.Ordinal))
        {
            claims.Add(new Claim(AuthTokenKeys.PermissionClaim, permission));
        }

        return claims;
    }
}
