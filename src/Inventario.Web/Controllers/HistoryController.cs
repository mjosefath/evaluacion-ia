using Inventario.ApiClient.Movements;
using Inventario.Contracts.Movements;
using Inventario.Contracts.Security;
using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

[Authorize(Policy = PermissionCodes.HistorialVer)]
[Route("history")]
public sealed class HistoryController(IInventarioMovementsApi movementsApi) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search,
        int? typeId,
        string? from,
        string? to,
        int page = 1,
        int pageSize = 20,
        string? sort = null,
        string? dir = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Historial";

        var fromUtc = LocalDateStartToUtc(from);
        var toUtc = LocalDateEndToUtc(to);

        var query = new MovementListQuery
        {
            Search = search,
            TypeId = typeId,
            From = fromUtc,
            To = toUtc,
            Page = page < 1 ? 1 : page,
            PageSize = pageSize is 20 or 50 or 100 ? pageSize : 20,
            Sort = string.IsNullOrWhiteSpace(sort) ? "date" : sort,
            Dir = string.IsNullOrWhiteSpace(dir) ? "desc" : dir
        };

        var result = await movementsApi.ListAsync(query, cancellationToken);

        string? typeLabel = null;
        if (typeId.HasValue)
        {
            typeLabel = result.Items.FirstOrDefault(i => i.MovementTypeId == typeId.Value)?.MovementTypeName
                        ?? typeId.Value.ToString();
        }

        return View(new HistoryIndexViewModel
        {
            Query = query,
            Result = result,
            FromLocal = NormalizeDateInput(from),
            ToLocal = NormalizeDateInput(to),
            TypeLabel = typeLabel
        });
    }

    /// <summary>Inicio del día local → UTC (inclusivo).</summary>
    private static DateTimeOffset? LocalDateStartToUtc(string? value)
    {
        if (!DateOnly.TryParse(value, out var date))
        {
            return null;
        }

        var local = date.ToDateTime(TimeOnly.MinValue);
        var offset = TimeZoneInfo.Local.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }

    /// <summary>Fin del día local → UTC (inclusivo).</summary>
    private static DateTimeOffset? LocalDateEndToUtc(string? value)
    {
        if (!DateOnly.TryParse(value, out var date))
        {
            return null;
        }

        var local = date.ToDateTime(new TimeOnly(23, 59, 59, 999));
        var offset = TimeZoneInfo.Local.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }

    private static string? NormalizeDateInput(string? value) =>
        DateOnly.TryParse(value, out var d) ? d.ToString("yyyy-MM-dd") : null;
}
