using Inventario.Api.Http;
using Inventario.Application.Reports;
using Inventario.Contracts.Common;
using Inventario.Contracts.Reports;
using Inventario.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController(IReportQueryService reports) : ControllerBase
{
    [HttpGet("stock")]
    [Authorize(Policy = PermissionCodes.ReporteVer)]
    [ProducesResponseType(typeof(ReportPagedResult<StockReportItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Stock(
        [FromQuery] StockReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await reports.ListStockAsync(query, cancellationToken);
        return ResultHttpMapper.ToActionResult(this, result);
    }
}
