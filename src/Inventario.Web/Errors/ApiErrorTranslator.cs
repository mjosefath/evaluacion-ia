using Inventario.ApiClient.Errors;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Inventario.Web.Errors;

/// <summary>
/// Traductor único ApiError → UI (análisis 03, W1). Los controladores no deciden por su cuenta.
/// </summary>
public static class ApiErrorTranslator
{
    public static ApiErrorTranslation Translate(ApiError error, ApiErrorSurface surface)
    {
        var code = error.Code ?? string.Empty;
        var detail = string.IsNullOrWhiteSpace(error.Detail) ? null : error.Detail;
        var traceId = error.TraceId;

        if (IsSessionExpired(error, code))
        {
            return new ApiErrorTranslation
            {
                Disposition = ApiErrorDisposition.RedirectLoginSessionExpired,
                Message = detail ?? WebUiMessages.SessionExpired,
                TraceId = traceId
            };
        }

        // Auth de login: siempre mensaje en formulario (W1).
        if (code is "auth.invalid_credentials" or "auth.locked" or "auth.inactive_profile")
        {
            if (surface == ApiErrorSurface.Form)
            {
                return FormMessage(detail ?? WebUiMessages.Unexpected, traceId);
            }

            // Fuera de login: inactive/locked con sesión rara → AccessDenied; credentials → login.
            if (code == "auth.invalid_credentials")
            {
                return new ApiErrorTranslation
                {
                    Disposition = ApiErrorDisposition.RedirectLoginSessionExpired,
                    Message = detail ?? WebUiMessages.SessionExpired,
                    TraceId = traceId
                };
            }

            return AccessDenied(detail, traceId);
        }

        return code switch
        {
            "validation" or "business.rule" or "conflict.duplicate" =>
                surface == ApiErrorSurface.Form
                    ? ModelStateFrom(error, detail, traceId)
                    : TempData(detail ?? WebUiMessages.Unexpected, traceId),

            "conflict.concurrency" =>
                TempData(detail ?? WebUiMessages.Unexpected, traceId),

            "not_found" =>
                surface == ApiErrorSurface.Form
                    ? TempData(detail ?? WebUiMessages.NotFound, traceId)
                    : NotFound(detail, traceId),

            "forbidden" =>
                surface == ApiErrorSurface.Form
                    ? FormMessage(detail ?? WebUiMessages.Forbidden, traceId)
                    : AccessDenied(detail, traceId),

            "unexpected" =>
                Unexpected(surface, detail, traceId),

            _ => ByStatus(error, surface, detail, traceId)
        };
    }

    public static void ApplyToModelState(
        ModelStateDictionary modelState,
        ApiErrorTranslation translation,
        Func<string, string>? mapField = null)
    {
        if (translation.FieldErrors is { Count: > 0 })
        {
            foreach (var (key, messages) in translation.FieldErrors)
            {
                var field = mapField?.Invoke(key) ?? key;
                foreach (var message in messages)
                {
                    modelState.AddModelError(field, message);
                }
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(translation.Message))
        {
            modelState.AddModelError(string.Empty, translation.Message);
        }
    }

    private static ApiErrorTranslation ByStatus(
        ApiError error,
        ApiErrorSurface surface,
        string? detail,
        string? traceId) =>
        error.Status switch
        {
            401 => new ApiErrorTranslation
            {
                Disposition = ApiErrorDisposition.RedirectLoginSessionExpired,
                Message = detail ?? WebUiMessages.SessionExpired,
                TraceId = traceId
            },
            403 => surface == ApiErrorSurface.Form
                ? FormMessage(detail ?? WebUiMessages.Forbidden, traceId)
                : AccessDenied(detail, traceId),
            404 => surface == ApiErrorSurface.Form
                ? TempData(detail ?? WebUiMessages.NotFound, traceId)
                : NotFound(detail, traceId),
            423 when surface == ApiErrorSurface.Form =>
                FormMessage(detail ?? WebUiMessages.Unexpected, traceId),
            >= 500 => Unexpected(surface, detail, traceId),
            _ when error.Errors is { Count: > 0 } && surface == ApiErrorSurface.Form =>
                ModelStateFrom(error, detail, traceId),
            _ when surface == ApiErrorSurface.Form =>
                FormMessage(detail ?? WebUiMessages.Unexpected, traceId),
            _ => Unexpected(ApiErrorSurface.Page, detail, traceId)
        };

    private static bool IsSessionExpired(ApiError error, string code) =>
        code == "auth.session_expired"
        || (error.Status == 401
            && code is not "auth.invalid_credentials"
            && code is not "auth.locked"
            && code is not "auth.inactive_profile");

    private static ApiErrorTranslation ModelStateFrom(ApiError error, string? detail, string? traceId) =>
        new()
        {
            Disposition = ApiErrorDisposition.ApplyModelState,
            Message = detail,
            TraceId = traceId,
            FieldErrors = error.Errors
        };

    private static ApiErrorTranslation FormMessage(string message, string? traceId) =>
        new()
        {
            Disposition = ApiErrorDisposition.FormMessage,
            Message = message,
            TraceId = traceId
        };

    private static ApiErrorTranslation TempData(string message, string? traceId) =>
        new()
        {
            Disposition = ApiErrorDisposition.TempDataError,
            Message = message,
            TraceId = traceId
        };

    private static ApiErrorTranslation AccessDenied(string? detail, string? traceId) =>
        new()
        {
            Disposition = ApiErrorDisposition.RedirectAccessDenied,
            Message = detail ?? WebUiMessages.Forbidden,
            TraceId = traceId
        };

    private static ApiErrorTranslation NotFound(string? detail, string? traceId) =>
        new()
        {
            Disposition = ApiErrorDisposition.RedirectNotFound,
            Message = detail ?? WebUiMessages.NotFound,
            TraceId = traceId
        };

    private static ApiErrorTranslation Unexpected(ApiErrorSurface surface, string? detail, string? traceId)
    {
        var message = WebUiMessages.UnexpectedWithReference(traceId);
        if (surface == ApiErrorSurface.Form)
        {
            return FormMessage(message, traceId);
        }

        return new ApiErrorTranslation
        {
            Disposition = ApiErrorDisposition.RedirectError,
            Message = detail ?? message,
            TraceId = traceId
        };
    }
}
