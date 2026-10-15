using Inventario.Data.System.Audit;

namespace Inventario.Data.System;

/// <summary>
/// Catálogo de tipos de movimiento (mutable). Codes: ENTRADA, SALIDA, AJUSTE.
/// </summary>
public sealed class MovementType : IMutableAudited
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public byte StatusId { get; set; } = RecordStatusIds.Active;

    public DateTime CreatedAtUtc { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public int? UpdatedBy { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
