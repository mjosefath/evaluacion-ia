namespace Inventario.Web.Errors;

/// <summary>
/// Textos solo-Web (M3): sin conexión, timeout e inesperado local. La API redacta el resto (M2).
/// </summary>
public static class WebUiMessages
{
    public const string ConnectionFailed =
        "No hay conexión con el servidor. Comprueba la red e inténtalo de nuevo.";

    public const string Timeout =
        "La operación tardó demasiado. Inténtalo de nuevo.";

    public const string Unexpected =
        "Algo salió mal. Inténtalo de nuevo.";

    public const string SessionExpired =
        "Tu sesión ha vencido. Inicia sesión de nuevo.";

    public const string Forbidden =
        "No tienes permiso para esta acción.";

    public const string NotFound =
        "No encontramos lo que buscas.";

    public static string WithReference(string message, string? traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
        {
            return message;
        }

        return $"{message} Código de referencia: {traceId}.";
    }

    public static string UnexpectedWithReference(string? traceId) =>
        WithReference(Unexpected, traceId);
}
