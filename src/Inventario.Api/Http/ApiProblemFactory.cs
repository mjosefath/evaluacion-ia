using System.Diagnostics;
using Inventario.Application.Results;
using Inventario.Contracts.Errors;

namespace Inventario.Api.Http;

/// <summary>
/// Construye <see cref="ApiProblem"/> (análisis 03, E3/E5/E7).
/// </summary>
internal static class ApiProblemFactory
{
    public static string ResolveTraceId(HttpContext httpContext) =>
        Activity.Current?.Id ?? httpContext.TraceIdentifier;

    public static int StatusForCode(string? code) => code switch
    {
        ErrorCodes.Validation or ErrorCodes.BusinessRule => StatusCodes.Status400BadRequest,
        ErrorCodes.AuthInvalidCredentials or ErrorCodes.AuthSessionExpired => StatusCodes.Status401Unauthorized,
        ErrorCodes.AuthLocked => StatusCodes.Status423Locked,
        ErrorCodes.AuthInactiveProfile or ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
        ErrorCodes.NotFound => StatusCodes.Status404NotFound,
        ErrorCodes.ConflictDuplicate or ErrorCodes.ConflictConcurrency => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    public static string TitleForCode(string? code) => code switch
    {
        ErrorCodes.Validation => "Datos inválidos",
        ErrorCodes.BusinessRule => "Regla de negocio",
        ErrorCodes.AuthInvalidCredentials => "Credenciales inválidas",
        ErrorCodes.AuthSessionExpired => "Sesión vencida",
        ErrorCodes.AuthLocked => "Cuenta bloqueada",
        ErrorCodes.AuthInactiveProfile => "Perfil inactivo",
        ErrorCodes.Forbidden => "Acceso denegado",
        ErrorCodes.NotFound => "No encontrado",
        ErrorCodes.ConflictDuplicate => "Registro duplicado",
        ErrorCodes.ConflictConcurrency => "Conflicto de concurrencia",
        ErrorCodes.Unexpected => "Error inesperado",
        _ => "Error"
    };

    public static ApiProblem FromResult(HttpContext httpContext, Result result)
    {
        var code = string.IsNullOrWhiteSpace(result.Code)
            ? ErrorCodes.Unexpected
            : result.Code;

        var status = StatusForCode(code);
        // E7: 500 solo mensaje genérico; nunca detalle interno.
        var detail = status >= 500
            ? MessageCatalog.Unexpected
            : (result.Message ?? MessageCatalog.ForCode(code, result.Rule));

        IDictionary<string, string[]>? errors = null;
        if (result.FieldErrors is { Count: > 0 })
        {
            errors = result.FieldErrors.ToDictionary(
                kv => kv.Key,
                kv => kv.Value,
                StringComparer.Ordinal);
        }

        return new ApiProblem
        {
            Type = $"https://inventario.local/errors/{code}",
            Title = TitleForCode(code),
            Status = status,
            Detail = detail,
            Code = code,
            Rule = code == ErrorCodes.BusinessRule ? result.Rule : null,
            Errors = errors,
            TraceId = ResolveTraceId(httpContext)
        };
    }

    public static ApiProblem Validation(
        HttpContext httpContext,
        IDictionary<string, string[]> errors,
        string? detail = null) =>
        new()
        {
            Type = $"https://inventario.local/errors/{ErrorCodes.Validation}",
            Title = TitleForCode(ErrorCodes.Validation),
            Status = StatusCodes.Status400BadRequest,
            Detail = detail ?? MessageCatalog.ValidationFailed,
            Code = ErrorCodes.Validation,
            Errors = errors,
            TraceId = ResolveTraceId(httpContext)
        };

    public static ApiProblem Unexpected(HttpContext httpContext) =>
        new()
        {
            Type = $"https://inventario.local/errors/{ErrorCodes.Unexpected}",
            Title = TitleForCode(ErrorCodes.Unexpected),
            Status = StatusCodes.Status500InternalServerError,
            Detail = MessageCatalog.Unexpected,
            Code = ErrorCodes.Unexpected,
            TraceId = ResolveTraceId(httpContext)
        };
}
