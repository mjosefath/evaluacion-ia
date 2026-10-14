using System.Diagnostics;
using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

public sealed class HomeController : Controller
{
    // AllowAnonymous temporal hasta T01-12 (cookie + [Authorize]).
    [AllowAnonymous]
    public IActionResult Index()
    {
        ViewData["Title"] = "Inicio";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
