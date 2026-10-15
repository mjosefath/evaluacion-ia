using Inventario.Data.System;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Application.Services;

public sealed class AccessLogWriter(InventarioSystemDbContext systemDb) : IAccessLogWriter
{
    public async Task WriteAsync(
        string eventType,
        string? identityUserId,
        string? userNameAttempted,
        string? ipAddress,
        string? userAgent,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        int? createdBy = null;
        if (!string.IsNullOrWhiteSpace(identityUserId))
        {
            createdBy = await systemDb.UserProfiles.AsNoTracking()
                .Where(p => p.IdentityUserId == identityUserId)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Detail nunca debe contener contraseñas ni tokens (R3).
        systemDb.AccessLogs.Add(new AccessLog
        {
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = createdBy,
            UserNameAttempted = Truncate(userNameAttempted, 256),
            EventType = eventType,
            IpAddress = Truncate(ipAddress, 64),
            UserAgent = Truncate(userAgent, 512),
            Detail = Truncate(detail, 500)
        });

        await systemDb.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
