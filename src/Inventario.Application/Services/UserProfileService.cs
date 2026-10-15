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
            IdentityUserId = identityUserId,
            DisplayName = string.IsNullOrWhiteSpace(userName) ? identityUserId : userName,
            StatusId = RecordStatusIds.Active,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = null
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
