namespace Inventario.Contracts.Auth;

/// <summary>
/// Mensajes y constantes de formato auth (V2). Una sola fuente para Web, API y unobtrusive.
/// <see cref="PasswordMinLength"/> alineado al default de ASP.NET Identity (<c>RequiredLength</c> = 6).
/// </summary>
public static class AuthValidationMessages
{
    /// <summary>Default Identity <c>PasswordOptions.RequiredLength</c>.</summary>
    public const int PasswordMinLength = 6;

    public const string CurrentPasswordRequired = "La contraseña actual es obligatoria.";
    public const string NewPasswordRequired = "La contraseña nueva es obligatoria.";
    public const string NewPasswordMinLength = "La contraseña nueva debe tener al menos {1} caracteres.";
    public const string ConfirmPasswordRequired = "La confirmación es obligatoria.";
    public const string ConfirmPasswordMismatch = "Las contraseñas no coinciden.";
}
