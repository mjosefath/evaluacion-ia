namespace Inventario.Contracts.Catalogs;

/// <summary>Fila de GET /api/movement-types (usp_MovementTypes_List).</summary>
public sealed record MovementTypeListItemDto(
    int Id,
    string Code,
    string Name,
    byte StatusId,
    string StatusName);
