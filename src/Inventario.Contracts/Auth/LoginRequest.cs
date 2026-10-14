namespace Inventario.Contracts.Auth;

/// <summary>
/// POST /api/auth/login. No loguear Password (R3).
/// </summary>
public sealed record LoginRequest(string UserName, string Password);
