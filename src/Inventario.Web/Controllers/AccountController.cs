using System.Text.Json;
using Inventario.ApiClient.Auth;
using Inventario.Contracts.Auth;
using Inventario.Contracts.Errors;
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
    public async Task<IActionResult> ChangePassword(
        ChangePasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await authApi.ChangePasswordAsync(
                new ChangePasswordRequest(model.CurrentPassword, model.NewPassword),
                cancellationToken);

            // ApiClient.ClearAsync ya limpia la cookie; reforzamos SignOut local.
            await authSession.SignOutAsync(cancellationToken);
            TempData["Success"] = "Contraseña actualizada. Inicia sesión de nuevo.";
            return RedirectToAction(nameof(Login));
        }
        catch (InventarioApiException ex) when (ex.StatusCode == 400)
        {
            ApplyChangePasswordApiErrors(ex);
            return View(model);
        }
        catch (InventarioApiException ex) when (ex.StatusCode is 401 or 403)
        {
            await authSession.SignOutAsync(cancellationToken);
            TempData["Error"] = "Tu sesión no es válida. Inicia sesión de nuevo.";
            return RedirectToAction(nameof(Login), new { sessionExpired = true });
        }
        catch (InventarioApiException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo cambiar la contraseña. Intenta de nuevo.");
            return View(model);
        }
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

    private void ApplyChangePasswordApiErrors(InventarioApiException ex)
    {
        if (string.IsNullOrWhiteSpace(ex.ResponseBody))
        {
            ModelState.AddModelError(string.Empty, "No se pudo cambiar la contraseña.");
            return;
        }

        try
        {
            var problem = JsonSerializer.Deserialize<ValidationProblemDetailsDto>(
                ex.ResponseBody,
                ApiErrorJsonOptions);

            if (problem?.Errors is { Count: > 0 })
            {
                foreach (var (key, messages) in problem.Errors)
                {
                    var field = MapChangePasswordField(key);
                    foreach (var message in messages)
                    {
                        ModelState.AddModelError(field, message);
                    }
                }

                return;
            }

            ModelState.AddModelError(
                string.Empty,
                problem?.Detail ?? problem?.Title ?? "No se pudo cambiar la contraseña.");
        }
        catch (JsonException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo cambiar la contraseña.");
        }
    }

    private static string MapChangePasswordField(string apiKey) =>
        apiKey.ToLowerInvariant() switch
        {
            "currentpassword" or "passwordmismatch" => nameof(ChangePasswordViewModel.CurrentPassword),
            "newpassword" or "password" => nameof(ChangePasswordViewModel.NewPassword),
            "confirmpassword" => nameof(ChangePasswordViewModel.ConfirmPassword),
            _ => string.Empty
        };

    private static readonly JsonSerializerOptions ApiErrorJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
