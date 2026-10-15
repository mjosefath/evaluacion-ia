using System.ComponentModel.DataAnnotations;
using Inventario.Contracts.Auth;

namespace Inventario.Web.Models;

/// <summary>
/// Formulario Web: hereda anotaciones de <see cref="ChangePasswordRequest"/> (V2 / A10).
/// ConfirmPassword solo en Web (no viaja a la API).
/// </summary>
public sealed class ChangePasswordViewModel : ChangePasswordRequest
{
    private string _confirmPassword = string.Empty;

    [Required(ErrorMessage = AuthValidationMessages.ConfirmPasswordRequired)]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    [Compare(nameof(NewPassword), ErrorMessage = AuthValidationMessages.ConfirmPasswordMismatch)]
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => _confirmPassword = (value ?? string.Empty).Trim();
    }

    public ChangePasswordRequest ToRequest() =>
        new()
        {
            CurrentPassword = CurrentPassword,
            NewPassword = NewPassword
        };
}
