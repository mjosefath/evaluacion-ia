using Inventario.Contracts.Common;
using Inventario.Contracts.Reports;

namespace Inventario.Web.Models;

public sealed class ReportStockViewModel
{
    public required StockReportQuery Query { get; init; }

    public required ReportPagedResult<StockReportItemDto> Result { get; init; }

    public string? CategoryLabel { get; init; }
}
