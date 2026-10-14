namespace Inventario.Data.System;

/// <summary>
/// Asignación rol Identity (por nombre) → permiso. Sin FK a BD Identity.
/// </summary>
public sealed class RolePermission
{
    public string RoleName { get; set; } = null!;

    public int PermissionId { get; set; }
}
