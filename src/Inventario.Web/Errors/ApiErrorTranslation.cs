namespace Inventario.Web.Errors;

public enum ApiErrorSurface
{
    /// <summary>Formulario o modal: ModelState / banner.</summary>
    Form,

    /// <summary>Pantalla completa / filtro global.</summary>
    Page
}

public enum ApiErrorDisposition
{
    ApplyModelState,
    FormMessage,
    TempDataError,
    RedirectLoginSessionExpired,
    RedirectAccessDenied,
    RedirectNotFound,
    RedirectError
}

/// <summary>Resultado del traductor W1.</summary>
public sealed class ApiErrorTranslation
{
    public required ApiErrorDisposition Disposition { get; init; }

    public string? Message { get; init; }

    public string? TraceId { get; init; }

    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; init; }
}
