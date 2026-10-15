using Inventario.ApiClient.Reports;
using Inventario.Contracts.Reports;
using Inventario.Contracts.Security;
using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

[Authorize(Policy = PermissionCodes.ReporteVer)]
[Route("reports")]
public sealed class ReportsController(IInventarioReportsApi reportsApi) : Controller
{
    [HttpGet("")]
    [HttpGet("stock")]
    public async Task<IActionResult> Stock(
        string? search,
        int? categoryId,
        int page = 1,
        int pageSize = 20,
        string? sort = null,
        string? dir = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Reporte";

        var query = new StockReportQuery
        {
            Search = search,
            CategoryId = categoryId,
            Page = page < 1 ? 1 : page,
            PageSize = pageSize is 20 or 50 or 100 ? pageSize : 20,
            Sort = string.IsNullOrWhiteSpace(sort) ? "code" : sort,
            Dir = string.IsNullOrWhiteSpace(dir) ? "asc" : dir
        };

        var result = await reportsApi.ListStockAsync(query, cancellationToken);

        string? categoryLabel = null;
        if (categoryId.HasValue)
        {
            categoryLabel = result.Items.FirstOrDefault(i => i.CategoryId == categoryId.Value)?.CategoryName
                            ?? categoryId.Value.ToString();
        }

        return View(new ReportStockViewModel
        {
            Query = query,
            Result = result,
            CategoryLabel = categoryLabel
        });
    }
}
