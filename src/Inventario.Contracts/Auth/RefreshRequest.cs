namespace Inventario.Contracts.Auth;

/// <summary>
/// POST /api/auth/refresh (anónimo). Envía el refresh en claro; la API solo persiste hash.
/// </summary>
public sealed record RefreshRequest(string RefreshToken);
