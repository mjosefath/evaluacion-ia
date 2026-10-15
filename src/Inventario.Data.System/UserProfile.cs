namespace Inventario.Data.System;

/// <summary>
/// Perfil en BD Sistema. Vínculo lógico a Identity por IdentityUserId (sin FK cross-database).
/// </summary>
public sealed class UserProfile
{
    public int Id { get; set; }

    public string IdentityUserId { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public byte StatusId { get; set; } = RecordStatusIds.Active;

    public DateTime CreatedAtUtc { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public int? UpdatedBy { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
