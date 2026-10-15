namespace Inventario.Web.Models;

public sealed class UiDemoFiltersViewModel
{
    public string? Search { get; init; }

    public required PaginatorModel Paginator { get; init; }
}
