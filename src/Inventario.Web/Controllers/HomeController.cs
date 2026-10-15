using System.Diagnostics;
using Inventario.ApiClient.Auth;
using Inventario.ApiClient.Movements;
using Inventario.ApiClient.Products;
using Inventario.ApiClient.Reports;
using Inventario.Contracts.Movements;
using Inventario.Contracts.Products;
using Inventario.Contracts.Reports;
using Inventario.Contracts.Security;
using Inventario.Web.Errors;
using Inventario.Web.Models;
using Inventario.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

public sealed class HomeController(
    IInventarioProductsApi productsApi,
    IInventarioMovementsApi movementsApi,
    IInventarioReportsApi reportsApi) : Controller
{
    [Authorize]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Inicio";

        var model = new HomeDashboardViewModel
        {
            DisplayName = User.Identity?.Name is { Length: > 0 } name && name != "pending"
                ? name
                : "Usuario",
            CanProducts = PermissionClaims.Has(User, PermissionCodes.ProductosVer),
            CanMovements = PermissionClaims.CanSeeMovimientos(User),
            CanHistory = PermissionClaims.Has(User, PermissionCodes.HistorialVer),
            CanReport = PermissionClaims.Has(User, PermissionCodes.ReporteVer)
        };

        int? activeProducts = null;
        int? lowStockCount = null;
        decimal? inventoryValue = null;
        int? movementsToday = null;
        IReadOnlyList<ProductListItemDto> lowStockItems = [];
        IReadOnlyList<MovementListItemDto> recentMovements = [];

        if (model.CanProducts)
        {
            try
            {
                var products = await productsApi.ListAsync(
                    new ProductListQuery
                    {
                        StatusId = 1,
                        Page = 1,
                        PageSize = 100,
                        Sort = "code",
                        Dir = "asc"
                    },
                    cancellationToken);

                activeProducts = products.TotalRows;
                var low = products.Items
                    .Where(p => string.Equals(p.StockStatus, "BAJO", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.Stock)
                    .ThenBy(p => p.Code)
                    .ToList();
                lowStockCount = low.Count;
                lowStockItems = low.Take(5).ToList();
            }
            catch (InventarioApiException)
            {
                // Insights parciales: no tumbar el home.
            }
        }

        if (model.CanReport)
        {
            try
            {
                var report = await reportsApi.ListStockAsync(
                    new StockReportQuery
                    {
                        Page = 1,
                        PageSize = 20,
                        Sort = "code",
                        Dir = "asc"
                    },
                    cancellationToken);
                inventoryValue = report.GrandTotal;
            }
            catch (InventarioApiException)
            {
            }
        }

        if (model.CanHistory)
        {
            try
            {
                var recent = await movementsApi.ListAsync(
                    new MovementListQuery
                    {
                        Page = 1,
                        PageSize = 6,
                        Sort = "date",
                        Dir = "desc"
                    },
                    cancellationToken);
                recentMovements = recent.Items;

                var todayLocal = DateOnly.FromDateTime(DateTime.Today);
                var fromLocal = todayLocal.ToDateTime(TimeOnly.MinValue);
                var toLocal = todayLocal.ToDateTime(new TimeOnly(23, 59, 59, 999));
                var fromUtc = new DateTimeOffset(fromLocal, TimeZoneInfo.Local.GetUtcOffset(fromLocal))
                    .ToUniversalTime();
                var toUtc = new DateTimeOffset(toLocal, TimeZoneInfo.Local.GetUtcOffset(toLocal))
                    .ToUniversalTime();

                var today = await movementsApi.ListAsync(
                    new MovementListQuery
                    {
                        Page = 1,
                        PageSize = 1,
                        Sort = "date",
                        Dir = "desc",
                        From = fromUtc,
                        To = toUtc
                    },
                    cancellationToken);
                movementsToday = today.TotalRows;
            }
            catch (InventarioApiException)
            {
            }
        }

        return View(
            new HomeDashboardViewModel
            {
                DisplayName = model.DisplayName,
                CanProducts = model.CanProducts,
                CanMovements = model.CanMovements,
                CanHistory = model.CanHistory,
                CanReport = model.CanReport,
                ActiveProducts = activeProducts,
                LowStockCount = lowStockCount,
                InventoryValue = inventoryValue,
                MovementsToday = movementsToday,
                LowStockItems = lowStockItems,
                RecentMovements = recentMovements
            });
    }

    /// <summary>Página 404 (nombre de acción NotFound; el método no se llama NotFound para no chocar con ControllerBase).</summary>
    [HttpGet]
    [ActionName("NotFound")]
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        ViewData["Message"] = WebUiMessages.NotFound;
        return View("NotFound");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error(string? traceId = null)
    {
        var reference = !string.IsNullOrWhiteSpace(traceId)
            ? traceId
            : Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        return View(new ErrorViewModel { RequestId = reference });
    }
}
