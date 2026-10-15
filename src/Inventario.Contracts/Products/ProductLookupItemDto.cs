namespace Inventario.Contracts.Products;

/// <summary>Fila de GET /api/products/lookup (usp_Products_Lookup).</summary>
public sealed record ProductLookupItemDto(
    int Id,
    string Code,
    string Name,
    int Stock);
