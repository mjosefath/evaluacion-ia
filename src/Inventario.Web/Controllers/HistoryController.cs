using Inventario.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

/// <summary>Stub RF03 / historial (pantalla en tarea posterior).</summary>
[Authorize(Policy = PermissionCodes.HistorialVer)]
[Route("history")]
public sealed class HistoryController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Historial";
        return View("~/Views/Shared/ComingSoon.cshtml");
    }
}
