namespace Inventario.Data.Identity;

/// <summary>
/// Refresh token persistido solo como hash (R3). Esquema: database/identity/001.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = null!;

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? ReplacedByTokenHash { get; set; }
}
