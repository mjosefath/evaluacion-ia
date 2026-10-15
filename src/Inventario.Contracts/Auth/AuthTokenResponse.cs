namespace Inventario.Contracts.Auth;

/// <summary>
/// Respuesta de login y refresh: tokens en claro para la sesión Web (nunca hashes de BD).
/// </summary>
public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset RefreshTokenExpiresAtUtc);
