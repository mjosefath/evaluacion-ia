using Inventario.Application.Results;
using Inventario.Contracts.Common;
using Inventario.Contracts.Reports;

namespace Inventario.Application.Reports;

public interface IReportQueryService
{
    Task<Result<ReportPagedResult<StockReportItemDto>>> ListStockAsync(
        StockReportQuery query,
        CancellationToken cancellationToken = default);
}
