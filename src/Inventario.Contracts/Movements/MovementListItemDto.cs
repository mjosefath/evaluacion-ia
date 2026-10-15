namespace Inventario.Contracts.Movements;

/// <summary>Fila de GET /api/movements (usp_InventoryMovements_List / historial).</summary>
public sealed record MovementListItemDto(
    long Id,
    DateTimeOffset CreatedAtUtc,
    int ProductId,
    string ProductCode,
    string ProductName,
    int MovementTypeId,
    string MovementTypeCode,
    string MovementTypeName,
    int Quantity,
    int StockBefore,
    int StockAfter,
    string? Reason,
    int? CreatedBy,
    string? CreatedByName);
