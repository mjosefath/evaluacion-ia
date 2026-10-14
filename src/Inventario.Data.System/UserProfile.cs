namespace Inventario.Data.System;

/// <summary>
/// Perfil en BD Sistema. Vínculo lógico a Identity por IdentityUserId (sin FK cross-database).
/// </summary>
public sealed class UserProfile
{
    public Guid Id { get; set; }

    public string IdentityUserId { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedByIdentityUserId { get; set; }

    public DateTime? ModifiedAtUtc { get; set; }

    public string? ModifiedByIdentityUserId { get; set; }
}
