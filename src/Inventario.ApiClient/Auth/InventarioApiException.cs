namespace Inventario.ApiClient.Auth;

/// <summary>
/// Fallo HTTP del ApiClient. No incluye tokens ni contraseñas en el mensaje.
/// </summary>
public sealed class InventarioApiException : Exception
{
    public InventarioApiException(int statusCode, string message, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public int StatusCode { get; }

    /// <summary>Cuerpo crudo (ProblemDetails). No debe usarse para logging de secretos.</summary>
    public string? ResponseBody { get; }

    /// <summary>True si el refresh silencioso falló o la sesión quedó inválida.</summary>
    public bool IsUnauthorized => StatusCode is 401 or 403;
}
