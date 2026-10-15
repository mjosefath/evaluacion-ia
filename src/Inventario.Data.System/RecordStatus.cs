namespace Inventario.Data.System;

/// <summary>
/// Catálogo de estatus del sistema (sin StatusId propio).
/// </summary>
public sealed class RecordStatus
{
    public byte Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;
}
