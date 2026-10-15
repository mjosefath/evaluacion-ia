using Inventario.Contracts.Common;

namespace Inventario.Contracts.Catalogs;

/// <summary>GET /api/movement-types?search&amp;statusId&amp;page&amp;pageSize&amp;sort&amp;dir</summary>
public sealed class MovementTypeListQuery : PagedQuery
{
    public byte? StatusId { get; set; }
}
