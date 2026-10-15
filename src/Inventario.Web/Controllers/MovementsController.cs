using Inventario.Contracts.Security;
using Inventario.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

/// <summary>Stub RF03 / captura de movimientos (pantalla en tarea posterior).</summary>
[Authorize]
[Route("movements")]
public sealed class MovementsController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        if (!PermissionClaims.CanSeeMovimientos(User))
        {
            return Forbid();
        }

        ViewData["Title"] = "Movimientos";
        return View("~/Views/Shared/ComingSoon.cshtml");
    }
}
