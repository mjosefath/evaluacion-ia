namespace Inventario.Contracts.Products;

/// <summary>Fila de GET /api/products (usp_Products_List).</summary>
public sealed record ProductListItemDto(
    int Id,
    string Code,
    string Name,
    int CategoryId,
    string CategoryName,
    decimal UnitPrice,
    int MinStock,
    int Stock,
    string StockStatus,
    byte StatusId,
    string StatusName,
    /// <summary>RowVersion en Base64 (contrato estable con el cliente).</summary>
    string RowVersion);
