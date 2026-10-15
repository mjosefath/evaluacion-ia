using Inventario.Contracts.Movements;
using Inventario.Contracts.Products;

namespace Inventario.Web.Models;

public sealed class HomeDashboardViewModel
{
    public string DisplayName { get; init; } = "Usuario";

    public bool CanProducts { get; init; }

    public bool CanMovements { get; init; }

    public bool CanHistory { get; init; }

    public bool CanReport { get; init; }

    public int? ActiveProducts { get; init; }

    public int? LowStockCount { get; init; }

    public decimal? InventoryValue { get; init; }

    public int? MovementsToday { get; init; }

    public IReadOnlyList<ProductListItemDto> LowStockItems { get; init; } = [];

    public IReadOnlyList<MovementListItemDto> RecentMovements { get; init; } = [];

    public bool HasAnyInsight =>
        ActiveProducts.HasValue
        || LowStockCount.HasValue
        || InventoryValue.HasValue
        || MovementsToday.HasValue
        || LowStockItems.Count > 0
        || RecentMovements.Count > 0;
}
