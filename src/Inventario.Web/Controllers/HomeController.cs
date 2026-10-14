using System.Diagnostics;
using Inventario.Web.Errors;
using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

public sealed class HomeController : Controller
{
    [Authorize]
    public IActionResult Index()
    {
        ViewData["Title"] = "Inicio";
        return View();
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
