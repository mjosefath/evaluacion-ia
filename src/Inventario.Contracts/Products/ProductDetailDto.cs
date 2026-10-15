namespace Inventario.Contracts.Products;

/// <summary>GET /api/products/{id}.</summary>
public sealed record ProductDetailDto(
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
    /// <summary>RowVersion en Base64.</summary>
    string RowVersion);
