using Inventario.Contracts.Common;

namespace Inventario.Contracts.Reports;

/// <summary>GET /api/reports/stock?search&amp;categoryId&amp;page&amp;pageSize&amp;sort&amp;dir</summary>
public sealed class StockReportQuery : PagedQuery
{
    public int? CategoryId { get; set; }
}
