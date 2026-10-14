using Inventario.ApiClient.Auth;
using Inventario.Contracts.Auth;
using Inventario.Web.Auth;
using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

public sealed class AccountController(
    IInventarioAuthApi authApi,
    IAuthTokenStore tokenStore,
    AuthSessionService authSession) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(bool sessionExpired = false, string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true
            && !string.Equals(User.Identity.Name, "pending", StringComparison.Ordinal))
        {
            return RedirectToLocal(returnUrl);
        }

        if (sessionExpired)
        {
            TempData["Error"] = "Tu sesión ha vencido. Inicia sesión de nuevo.";
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var tokens = await authApi.LoginAsync(
                new LoginRequest(model.UserName.Trim(), model.Password),
                cancellationToken);

            var me = await authApi.MeAsync(cancellationToken);
            await authSession.EstablishAsync(me, tokens, cancellationToken);

            return RedirectToLocal(returnUrl);
        }
        catch (InventarioApiException ex) when (ex.StatusCode == 401)
        {
            await authSession.SignOutAsync(cancellationToken);
            model.ErrorMessage = "Credenciales inválidas.";
            return View(model);
        }
        catch (InventarioApiException ex) when (ex.StatusCode == 423)
        {
            await authSession.SignOutAsync(cancellationToken);
            model.ErrorMessage = "Cuenta bloqueada temporalmente. Intenta más tarde.";
            return View(model);
        }
        catch (InventarioApiException ex) when (ex.StatusCode == 403)
        {
            await authSession.SignOutAsync(cancellationToken);
            model.ErrorMessage = "Perfil inactivo. Contacta al administrador.";
            return View(model);
        }
        catch (InventarioApiException)
        {
            await authSession.SignOutAsync(cancellationToken);
            model.ErrorMessage = "No se pudo iniciar sesión. Intenta de nuevo.";
            return View(model);
        }
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Cableado a API en tarea posterior de cambio de contraseña.
        TempData["Info"] = "El cambio de contraseña vía API se completará en una tarea siguiente.";
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refresh = await tokenStore.GetRefreshTokenAsync(cancellationToken);

        try
        {
            if (!string.IsNullOrWhiteSpace(refresh))
            {
                await authApi.LogoutAsync(new LogoutRequest(refresh), cancellationToken);
            }
        }
        catch (InventarioApiException)
        {
            // Revocación remota falló: igual se limpia la sesión local.
        }

        await authSession.SignOutAsync(cancellationToken);
        TempData["Info"] = "Sesión cerrada.";
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(HomeController.Index), "Home");
}
