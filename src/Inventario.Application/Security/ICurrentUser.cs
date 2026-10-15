namespace Inventario.Application.Security;

/// <summary>
/// Usuario de la petición actual (API). UserProfileId alimenta CreatedBy/UpdatedBy (E5).
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>Identity user id (string), o null si anónimo.</summary>
    string? UserId { get; }

    /// <summary>
    /// Claim <c>profile_id</c> si viene en el token; si no, el valor cacheado tras
    /// <see cref="ResolveUserProfileIdAsync"/>.
    /// </summary>
    int? UserProfileId { get; }

    /// <summary>
    /// Resuelve el perfil activo: claim <c>profile_id</c> o lookup por IdentityUserId.
    /// Null si no hay sesión, no hay perfil o está inactivo.
    /// </summary>
    Task<int?> ResolveUserProfileIdAsync(CancellationToken cancellationToken = default);

    /// <summary>True si el principal tiene el claim de permiso indicado.</summary>
    bool HasPermission(string permissionCode);
}
