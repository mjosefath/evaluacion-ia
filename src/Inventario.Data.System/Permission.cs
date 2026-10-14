namespace Inventario.Data.System;

public sealed class Permission
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Module { get; set; } = null!;
}
