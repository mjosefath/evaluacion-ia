using Inventario.Contracts.Common;
using Inventario.Contracts.Movements;

namespace Inventario.Web.Models;

public sealed class HistoryIndexViewModel
{
    public required MovementListQuery Query { get; init; }

    public required PagedResult<MovementListItemDto> Result { get; init; }

    /// <summary>Fecha local (yyyy-MM-dd) del filtro desde, para el input.</summary>
    public string? FromLocal { get; init; }

    /// <summary>Fecha local (yyyy-MM-dd) del filtro hasta.</summary>
    public string? ToLocal { get; init; }

    public string? TypeLabel { get; init; }
}
