using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

/// <summary>
/// Vistas de cuenta. Auth real contra ApiClient en T01-12.
/// AllowAnonymous temporal en Inicio/Login hasta cablear sesión.
/// </summary>
public sealed class AccountController : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Placeholder T01-11: sin llamada a API aún.
        TempData["Info"] = "El inicio de sesión se cableará en la siguiente tarea.";
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        TempData["Info"] = "El cambio de contraseña se cableará en la siguiente tarea.";
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        TempData["Info"] = "El cierre de sesión se cableará en la siguiente tarea.";
        return RedirectToAction(nameof(Login));
    }
}
