using Inventario.Contracts.Auth;

namespace Inventario.ApiClient.Auth;

/// <summary>
/// Almacén de tokens de sesión. La Web lo implementa (p. ej. cookie); el ApiClient no conoce cookies.
/// </summary>
public interface IAuthTokenStore
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default);

    Task SetTokensAsync(AuthTokenResponse tokens, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
