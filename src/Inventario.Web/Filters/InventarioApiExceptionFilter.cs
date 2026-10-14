using Inventario.ApiClient.Auth;
using Inventario.Web.Auth;
using Inventario.Web.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Inventario.Web.Filters;

/// <summary>
/// Traduce <see cref="InventarioApiException"/> no capturada según W1 (página completa).
/// 401 → login; 403 → AccessDenied (nunca Login).
/// </summary>
public sealed class InventarioApiExceptionFilter(
    AuthSessionService authSession,
    ITempDataDictionaryFactory tempDataFactory) : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not InventarioApiException apiEx)
        {
            return;
        }

        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();
        if (string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase)
            && string.Equals(action, "Login", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var translation = ApiErrorTranslator.Translate(apiEx.Error, ApiErrorSurface.Page);

        switch (translation.Disposition)
        {
            case ApiErrorDisposition.RedirectLoginSessionExpired:
                await authSession.SignOutAsync(context.HttpContext.RequestAborted);
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Account",
                    new { sessionExpired = true });
                break;

            case ApiErrorDisposition.RedirectAccessDenied:
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                break;

            case ApiErrorDisposition.RedirectNotFound:
                context.Result = new RedirectToActionResult("NotFound", "Home", null);
                break;

            case ApiErrorDisposition.RedirectError:
                context.Result = new RedirectToActionResult(
                    "Error",
                    "Home",
                    new { traceId = translation.TraceId ?? apiEx.Error.TraceId });
                break;

            case ApiErrorDisposition.TempDataError:
            case ApiErrorDisposition.FormMessage:
            case ApiErrorDisposition.ApplyModelState:
                var tempData = tempDataFactory.GetTempData(context.HttpContext);
                tempData["Error"] = translation.Message
                    ?? apiEx.Error.Detail
                    ?? WebUiMessages.Unexpected;
                context.Result = new RedirectToActionResult("Index", "Home", null);
                break;

            default:
                return;
        }

        context.ExceptionHandled = true;
    }
}
