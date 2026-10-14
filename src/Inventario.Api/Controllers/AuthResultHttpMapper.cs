using Inventario.Application.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Controllers;

/// <summary>
/// Traduce AuthResult a HTTP según el análisis y ADR-002.
/// </summary>
internal static class AuthResultHttpMapper
{
    public static IActionResult ToActionResult<T>(ControllerBase controller, AuthResult<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Value is null
                ? controller.NoContent()
                : controller.Ok(result.Value);
        }

        return result.ErrorCode switch
        {
            AuthErrorCode.Validation => Validation(controller, result),
            AuthErrorCode.PasswordChangeFailed => Validation(controller, result),
            AuthErrorCode.InvalidCredentials => Problem(
                controller,
                StatusCodes.Status401Unauthorized,
                "Credenciales inválidas."),
            AuthErrorCode.Lockout => Problem(
                controller,
                StatusCodes.Status423Locked,
                result.ErrorMessage ?? "Cuenta bloqueada temporalmente."),
            AuthErrorCode.InactiveProfile => Problem(
                controller,
                StatusCodes.Status403Forbidden,
                result.ErrorMessage ?? "Perfil inactivo."),
            AuthErrorCode.Unauthorized or AuthErrorCode.InvalidRefreshToken => Problem(
                controller,
                StatusCodes.Status401Unauthorized,
                result.ErrorMessage ?? "No autenticado."),
            AuthErrorCode.NotFound => Problem(
                controller,
                StatusCodes.Status404NotFound,
                result.ErrorMessage ?? "No encontrado."),
            _ => Problem(
                controller,
                StatusCodes.Status500InternalServerError,
                "Error inesperado.")
        };
    }

    private static IActionResult Validation<T>(ControllerBase controller, AuthResult<T> result)
    {
        var errors = result.FieldErrors is { Count: > 0 }
            ? result.FieldErrors.ToDictionary(kv => kv.Key, kv => kv.Value)
            : new Dictionary<string, string[]>
            {
                [string.Empty] = [result.ErrorMessage ?? "Datos de entrada inválidos."]
            };

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = result.ErrorMessage ?? "Uno o más errores de validación.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        };

        return controller.ValidationProblem(problem);
    }

    private static IActionResult Problem(ControllerBase controller, int statusCode, string detail) =>
        controller.Problem(
            detail: detail,
            statusCode: statusCode,
            title: ReasonPhrase(statusCode));

    private static string ReasonPhrase(int statusCode) => statusCode switch
    {
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status423Locked => "Locked",
        _ => "Error"
    };
}
