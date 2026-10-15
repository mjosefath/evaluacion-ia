using Inventario.ApiClient.Auth;
using Inventario.Contracts.Auth;
using Inventario.Web.Auth;
using Inventario.Web.Errors;
using Inventario.Web.Feedback;
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
            FlashMessages.SetError(TempData, WebUiMessages.SessionExpired);
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
        catch (InventarioApiException ex)
        {
            await authSession.SignOutAsync(cancellationToken);
            var translation = ApiErrorTranslator.Translate(ex.Error, ApiErrorSurface.Form);
            model.ErrorMessage = translation.Message
                ?? ex.Error.Detail
                ?? WebUiMessages.Unexpected;
            return View(model);
        }
        catch (HttpRequestException)
        {
            await authSession.SignOutAsync(cancellationToken);
            model.ErrorMessage = WebUiMessages.ConnectionFailed;
            return View(model);
        }
        catch (TaskCanceledException)
        {
            await authSession.SignOutAsync(cancellationToken);
            model.ErrorMessage = WebUiMessages.Timeout;
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        ViewData["Message"] = WebUiMessages.Forbidden;
        return View();
    }

    /// <summary>
    /// La UI de cambio de contraseña vive en el action-modal (T03-02). GET ya no sirve página.
    /// </summary>
    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() =>
        RedirectToAction(nameof(HomeController.Index), "Home");

    /// <summary>
    /// POST del modal: AJAX → partial HTML con errores o JSON { redirectUrl }.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ChangePasswordFormResult(model, statusCode: 400);
        }

        try
        {
            await authApi.ChangePasswordAsync(model.ToRequest(), cancellationToken);

            await authSession.SignOutAsync(cancellationToken);
            FlashMessages.SetSuccess(TempData, "Contraseña actualizada. Inicia sesión de nuevo.");

            if (IsModalAjaxRequest())
            {
                return Json(new { redirectUrl = Url.Action(nameof(Login)) });
            }

            return RedirectToAction(nameof(Login));
        }
        catch (InventarioApiException ex)
        {
            return await HandleChangePasswordApiErrorAsync(model, ex, cancellationToken);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, WebUiMessages.ConnectionFailed);
            return ChangePasswordFormResult(model, statusCode: 400);
        }
        catch (TaskCanceledException)
        {
            ModelState.AddModelError(string.Empty, WebUiMessages.Timeout);
            return ChangePasswordFormResult(model, statusCode: 400);
        }
    }

    private async Task<IActionResult> HandleChangePasswordApiErrorAsync(
        ChangePasswordViewModel model,
        InventarioApiException ex,
        CancellationToken cancellationToken)
    {
        var translation = ApiErrorTranslator.Translate(ex.Error, ApiErrorSurface.Form);

        switch (translation.Disposition)
        {
            case ApiErrorDisposition.ApplyModelState:
                ApiErrorTranslator.ApplyToModelState(ModelState, translation, MapChangePasswordField);
                return ChangePasswordFormResult(model, statusCode: 400);

            case ApiErrorDisposition.FormMessage:
            case ApiErrorDisposition.TempDataError:
                ModelState.AddModelError(
                    string.Empty,
                    translation.Message ?? ex.Error.Detail ?? WebUiMessages.Unexpected);
                return ChangePasswordFormResult(model, statusCode: ex.StatusCode is >= 400 and < 600 ? ex.StatusCode : 400);

            case ApiErrorDisposition.RedirectLoginSessionExpired:
                await authSession.SignOutAsync(cancellationToken);
                FlashMessages.SetError(TempData, translation.Message ?? WebUiMessages.SessionExpired);

                if (IsModalAjaxRequest())
                {
                    return Json(new
                    {
                        redirectUrl = Url.Action(nameof(Login), new { sessionExpired = true })
                    });
                }

                return RedirectToAction(nameof(Login), new { sessionExpired = true });

            case ApiErrorDisposition.RedirectAccessDenied:
                if (IsModalAjaxRequest())
                {
                    return Json(new { redirectUrl = Url.Action(nameof(AccessDenied)) });
                }

                return RedirectToAction(nameof(AccessDenied));

            case ApiErrorDisposition.RedirectNotFound:
                if (IsModalAjaxRequest())
                {
                    return Json(new { redirectUrl = Url.Action("NotFound", "Home") });
                }

                return RedirectToAction("NotFound", "Home");

            case ApiErrorDisposition.RedirectError:
                if (IsModalAjaxRequest())
                {
                    return Json(new
                    {
                        redirectUrl = Url.Action(
                            nameof(HomeController.Error),
                            "Home",
                            new { traceId = translation.TraceId ?? ex.Error.TraceId })
                    });
                }

                return RedirectToAction(
                    nameof(HomeController.Error),
                    "Home",
                    new { traceId = translation.TraceId ?? ex.Error.TraceId });

            default:
                ModelState.AddModelError(string.Empty, WebUiMessages.Unexpected);
                return ChangePasswordFormResult(model, statusCode: 400);
        }
    }

    private bool IsModalAjaxRequest() =>
        string.Equals(
            Request.Headers.XRequestedWith.ToString(),
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);

    private IActionResult ChangePasswordFormResult(ChangePasswordViewModel model, int statusCode)
    {
        Response.StatusCode = statusCode;
        return PartialView("~/Views/Account/_ChangePasswordForm.cshtml", model);
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
        FlashMessages.SetInfo(TempData, "Sesión cerrada.");
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(HomeController.Index), "Home");

    private static string MapChangePasswordField(string apiKey) =>
        apiKey.ToLowerInvariant() switch
        {
            "currentpassword" or "passwordmismatch" => nameof(ChangePasswordViewModel.CurrentPassword),
            "newpassword" or "password" => nameof(ChangePasswordViewModel.NewPassword),
            "confirmpassword" => nameof(ChangePasswordViewModel.ConfirmPassword),
            _ => string.Empty
        };
}
