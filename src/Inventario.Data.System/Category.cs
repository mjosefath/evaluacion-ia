using Inventario.Data.System.Audit;

namespace Inventario.Data.System;

/// <summary>
/// Catálogo de categorías (mutable). Seed: Electrónica, Papelería, Limpieza.
/// </summary>
public sealed class Category : IMutableAudited
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public byte StatusId { get; set; } = RecordStatusIds.Active;

    public DateTime CreatedAtUtc { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public int? UpdatedBy { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
