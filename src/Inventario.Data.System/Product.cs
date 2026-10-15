using Inventario.Data.System.Audit;

namespace Inventario.Data.System;

/// <summary>
/// Producto (mutable). Existencia = <see cref="ProductStock"/> / último movimiento; no hay columna Stock.
/// </summary>
public sealed class Product : IMutableAudited
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int CategoryId { get; set; }

    public decimal UnitPrice { get; set; }

    public int MinStock { get; set; }

    public byte StatusId { get; set; } = RecordStatusIds.Active;

    public DateTime CreatedAtUtc { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public int? UpdatedBy { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
