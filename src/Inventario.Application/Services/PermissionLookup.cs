using Inventario.Data.System;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Application.Services;

public sealed class PermissionLookup(InventarioSystemDbContext systemDb) : IPermissionLookup
{
    public async Task<IReadOnlyList<string>> GetPermissionCodesForRolesAsync(
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        var roles = roleNames
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (roles.Length == 0)
        {
            return Array.Empty<string>();
        }

        return await systemDb.RolePermissions.AsNoTracking()
            .Where(rp =>
                roles.Contains(rp.RoleName)
                && rp.StatusId == RecordStatusIds.Active)
            .Join(
                systemDb.Permissions.AsNoTracking()
                    .Where(p => p.StatusId == RecordStatusIds.Active),
                rp => rp.PermissionId,
                p => p.Id,
                (_, p) => p.Code)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }
}
