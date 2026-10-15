namespace Inventario.Contracts.Catalogs;

/// <summary>Fila de GET /api/record-statuses (usp_RecordStatuses_List).</summary>
public sealed record RecordStatusListItemDto(
    byte Id,
    string Code,
    string Name);
