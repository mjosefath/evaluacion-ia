using Inventario.Contracts.Common;

namespace Inventario.Contracts.Movements;

/// <summary>GET /api/movements?search&amp;typeId&amp;from&amp;to&amp;page&amp;pageSize&amp;sort&amp;dir</summary>
public sealed class MovementListQuery : PagedQuery
{
    public int? TypeId { get; set; }

    /// <summary>Fecha/hora desde (UTC), inclusiva.</summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>Fecha/hora hasta (UTC), inclusiva.</summary>
    public DateTimeOffset? To { get; set; }
}
