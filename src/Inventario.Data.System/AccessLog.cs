using Inventario.Data.System.Audit;

namespace Inventario.Data.System;

/// <summary>
/// Bitácora de accesos (inmutable). EventType: LoginSuccess, LoginFailure, Lockout, Logout, TokenRefresh, PasswordChanged.
/// </summary>
public sealed class AccessLog : IImmutableAudited
{
    public long Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>FK a UserProfiles.Id; NULL si no hay perfil (p. ej. login fallido).</summary>
    public int? CreatedBy { get; set; }

    public string? UserNameAttempted { get; set; }

    public string EventType { get; set; } = null!;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? Detail { get; set; }
}
