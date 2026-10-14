namespace Inventario.Contracts.Auth;

/// <summary>
/// POST /api/auth/logout (Bearer). Body con el refresh actual a revocar.
/// </summary>
public sealed record LogoutRequest(string RefreshToken);
