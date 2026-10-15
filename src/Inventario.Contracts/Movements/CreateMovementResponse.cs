namespace Inventario.Contracts.Movements;

/// <summary>Respuesta de POST /api/movements (aviso de éxito / R13).</summary>
public sealed record CreateMovementResponse(
    long Id,
    int ProductId,
    string ProductCode,
    string ProductName,
    string MovementTypeCode,
    int Quantity,
    int StockBefore,
    int StockAfter,
    /// <summary>true si quedó en BAJO (existencia ≤ mínimo); la Web muestra advertencia W10.</summary>
    bool IsLowStock,
    int MinStock);
