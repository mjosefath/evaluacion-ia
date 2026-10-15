namespace Inventario.Contracts.Reports;

/// <summary>Fila de GET /api/reports/stock (usp_Report_Stock).</summary>
public sealed record StockReportItemDto(
    int ProductId,
    string Code,
    string Name,
    int CategoryId,
    string CategoryName,
    decimal UnitPrice,
    int Stock,
    int MinStock,
    string StockStatus,
    decimal InventoryValue);
