namespace Inventario.Data.System;

/// <summary>
/// Movimiento de inventario (inmutable). Quantity con signo = efecto sobre existencia.
/// </summary>
public sealed class InventoryMovement
{
    public long Id { get; set; }

    public int ProductId { get; set; }

    public int MovementTypeId { get; set; }

    public int Quantity { get; set; }

    public int StockBefore { get; set; }

    public int StockAfter { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public int? CreatedBy { get; set; }
}
