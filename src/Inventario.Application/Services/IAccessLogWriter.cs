namespace Inventario.Application.Services;

public interface IAccessLogWriter
{
    Task WriteAsync(
        string eventType,
        string? identityUserId,
        string? userNameAttempted,
        string? ipAddress,
        string? userAgent,
        string? detail = null,
        CancellationToken cancellationToken = default);
}
