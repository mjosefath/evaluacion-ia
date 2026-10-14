using System.ComponentModel.DataAnnotations;

namespace Inventario.Web.Models;

public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña nueva")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
