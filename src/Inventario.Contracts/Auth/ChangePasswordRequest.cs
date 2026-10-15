using System.ComponentModel.DataAnnotations;

namespace Inventario.Contracts.Auth;

/// <summary>
/// POST /api/auth/change-password (Bearer). No loguear contraseñas (R3).
/// Reglas/mensajes de formato aquí (V2); la confirmación vive solo en la Web.
/// </summary>
public class ChangePasswordRequest
{
    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;

    [Required(ErrorMessage = AuthValidationMessages.CurrentPasswordRequired)]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string CurrentPassword
    {
        get => _currentPassword;
        // V6: trim; solo espacios → vacío → Required falla.
        set => _currentPassword = (value ?? string.Empty).Trim();
    }

    [Required(ErrorMessage = AuthValidationMessages.NewPasswordRequired)]
    [MinLength(
        AuthValidationMessages.PasswordMinLength,
        ErrorMessage = AuthValidationMessages.NewPasswordMinLength)]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña nueva")]
    public string NewPassword
    {
        get => _newPassword;
        set => _newPassword = (value ?? string.Empty).Trim();
    }
}
