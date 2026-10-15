using Inventario.Contracts.Common;
using Inventario.Contracts.Reports;

namespace Inventario.ApiClient.Reports;

public interface IInventarioReportsApi
{
    Task<ReportPagedResult<StockReportItemDto>> ListStockAsync(
        StockReportQuery query,
        CancellationToken cancellationToken = default);
}
