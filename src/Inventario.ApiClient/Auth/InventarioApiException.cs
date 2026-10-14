using Inventario.ApiClient.Errors;

namespace Inventario.ApiClient.Auth;

/// <summary>
/// Fallo HTTP del ApiClient con <see cref="ApiError"/> tipado. No incluye tokens ni contraseñas.
/// </summary>
public sealed class InventarioApiException : Exception
{
    public InventarioApiException(ApiError error)
        : base(error.Detail ?? $"La API respondió {error.Status}.")
    {
        Error = error;
    }

    public ApiError Error { get; }

    public int StatusCode => Error.Status;

    /// <summary>Sin sesión / token inválido (solo 401). No incluye 403.</summary>
    public bool IsUnauthorized => StatusCode == 401;

    /// <summary>Sesión válida sin permiso (solo 403).</summary>
    public bool IsForbidden => StatusCode == 403;
}
