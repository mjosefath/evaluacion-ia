namespace Inventario.Data.System;

/// <summary>
/// Asignación rol Identity (por nombre) → permiso. Sin FK a BD Identity.
/// Revocar = StatusId = Inactivo (E1).
/// </summary>
public sealed class RolePermission
{
    public int Id { get; set; }

    public string RoleName { get; set; } = null!;

    public int PermissionId { get; set; }

    public byte StatusId { get; set; } = RecordStatusIds.Active;

    public DateTime CreatedAtUtc { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public int? UpdatedBy { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
