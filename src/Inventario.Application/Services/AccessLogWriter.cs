using Inventario.Data.System;

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
        // Detail nunca debe contener contraseñas ni tokens (R3).
        systemDb.AccessLogs.Add(new AccessLog
        {
            OccurredAtUtc = DateTime.UtcNow,
            IdentityUserId = identityUserId,
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
