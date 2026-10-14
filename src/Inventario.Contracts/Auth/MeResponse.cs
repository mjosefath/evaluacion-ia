namespace Inventario.Contracts.Auth;

/// <summary>
/// GET /api/auth/me — hidrata UI (DisplayName, roles, códigos de permiso).
/// </summary>
public sealed record MeResponse(
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
