using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventario.Web.Navigation;

/// <summary>
/// Activo del sidebar por ruta actual (controller/action). No usa localStorage.
/// Abrir modales (p. ej. ChangePassword) no cambia la ruta ni el ítem activo.
/// </summary>
public static class NavActive
{
    public static bool IsMatch(ViewContext viewContext, string controller, string? action = null)
    {
        ArgumentNullException.ThrowIfNull(viewContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(controller);

        var currentController = viewContext.RouteData.Values["controller"]?.ToString() ?? string.Empty;
        if (!string.Equals(currentController, controller, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (action is null)
        {
            return true;
        }

        var currentAction = viewContext.RouteData.Values["action"]?.ToString() ?? string.Empty;
        return string.Equals(currentAction, action, StringComparison.OrdinalIgnoreCase);
    }
}
