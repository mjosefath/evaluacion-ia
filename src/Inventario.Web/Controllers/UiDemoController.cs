using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

/// <summary>
/// Demo interna de barra de filtros + paginador + Tom Select (T02-13).
/// Sin lógica de negocio de productos (T02-14).
/// </summary>
[Authorize]
[Route("ui-demo")]
public sealed class UiDemoController : Controller
{
    [HttpGet("")]
    [HttpGet("filters")]
    public IActionResult Filters(
        string? search,
        int page = 1,
        int pageSize = 20)
    {
        ViewData["Title"] = "Demo filtros / selectores";

        return View(new UiDemoFiltersViewModel
        {
            Search = search,
            Paginator = new PaginatorModel
            {
                Page = page < 1 ? 1 : page,
                PageSize = pageSize is 20 or 50 or 100 ? pageSize : 20,
                TotalRows = 0
            }
        });
    }
}
