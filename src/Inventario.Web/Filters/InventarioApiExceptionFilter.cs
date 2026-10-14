using Inventario.ApiClient.Auth;
using Inventario.Web.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Inventario.Web.Filters;

/// <summary>
/// Si el ApiClient recibe 401 tras fallar el refresh: limpia cookie y redirige a Login.
/// </summary>
public sealed class InventarioApiExceptionFilter(AuthSessionService authSession) : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not InventarioApiException { StatusCode: 401 })
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

        await authSession.SignOutAsync(context.HttpContext.RequestAborted);

        context.Result = new RedirectToActionResult(
            "Login",
            "Account",
            new { sessionExpired = true });
        context.ExceptionHandled = true;
    }
}
