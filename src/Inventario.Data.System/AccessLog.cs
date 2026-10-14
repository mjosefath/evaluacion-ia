namespace Inventario.Data.System;

/// <summary>
/// Bitácora de accesos. EventType: LoginSuccess, LoginFailure, Lockout, Logout, TokenRefresh, PasswordChanged.
/// </summary>
public sealed class AccessLog
{
    public long Id { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public string? IdentityUserId { get; set; }

    public string? UserNameAttempted { get; set; }

    public string EventType { get; set; } = null!;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? Detail { get; set; }
}
