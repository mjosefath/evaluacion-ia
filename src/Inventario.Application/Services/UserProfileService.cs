using Inventario.Data.System;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Application.Services;

public sealed class UserProfileService(InventarioSystemDbContext systemDb) : IUserProfileService
{
    public Task<UserProfile?> FindByIdentityUserIdAsync(
        string identityUserId,
        CancellationToken cancellationToken = default) =>
        systemDb.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdentityUserId == identityUserId, cancellationToken);

    public async Task<UserProfile> GetOrCreateAsync(
        string identityUserId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var existing = await systemDb.UserProfiles
            .FirstOrDefaultAsync(p => p.IdentityUserId == identityUserId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            IdentityUserId = identityUserId,
            DisplayName = string.IsNullOrWhiteSpace(userName) ? identityUserId : userName,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIdentityUserId = null
        };

        systemDb.UserProfiles.Add(profile);

        try
        {
            await systemDb.SaveChangesAsync(cancellationToken);
            return profile;
        }
        catch (DbUpdateException)
        {
            // Carrera en índice único: otro request creó el perfil.
            systemDb.ChangeTracker.Clear();
            var created = await systemDb.UserProfiles
                .FirstOrDefaultAsync(p => p.IdentityUserId == identityUserId, cancellationToken);

            if (created is null)
            {
                throw;
            }

            return created;
        }
    }
}
