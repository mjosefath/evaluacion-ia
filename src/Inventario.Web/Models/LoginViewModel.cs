using System.ComponentModel.DataAnnotations;

namespace Inventario.Web.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [Display(Name = "Usuario")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}
