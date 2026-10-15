namespace Inventario.Contracts.Catalogs;

/// <summary>Fila de GET /api/categories (usp_Categories_List).</summary>
public sealed record CategoryListItemDto(
    int Id,
    string Name,
    byte StatusId,
    string StatusName);
