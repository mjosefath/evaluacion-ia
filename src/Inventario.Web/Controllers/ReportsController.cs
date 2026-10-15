using Inventario.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

/// <summary>Stub RF03 / reporte (pantalla en tarea posterior).</summary>
[Authorize(Policy = PermissionCodes.ReporteVer)]
[Route("reports")]
public sealed class ReportsController : Controller
{
    [HttpGet("")]
    [HttpGet("stock")]
    public IActionResult Stock()
    {
        ViewData["Title"] = "Reporte";
        return View("~/Views/Shared/ComingSoon.cshtml");
    }
}
