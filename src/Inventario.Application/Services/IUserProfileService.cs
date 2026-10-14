using Inventario.Data.System;

namespace Inventario.Application.Services;

public interface IUserProfileService
{
    Task<UserProfile?> FindByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el perfil o lo crea con DisplayName = userName e IsActive = true (idempotente).
    /// </summary>
    Task<UserProfile> GetOrCreateAsync(string identityUserId, string userName, CancellationToken cancellationToken = default);
}
