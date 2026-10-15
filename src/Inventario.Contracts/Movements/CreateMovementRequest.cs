using System.ComponentModel.DataAnnotations;

namespace Inventario.Contracts.Movements;

/// <summary>
/// POST /api/movements. <see cref="Type"/> = Code de MovementTypes (ENTRADA, SALIDA, AJUSTE).
/// Reglas de signo/motivo las aplica Application (R3–R4).
/// </summary>
public sealed class CreateMovementRequest
{
    private string _type = string.Empty;
    private string? _reason;

    [Required(ErrorMessage = MovementValidationMessages.TypeRequired)]
    [MaxLength(10, ErrorMessage = MovementValidationMessages.TypeMaxLength)]
    [Display(Name = "Tipo")]
    public string Type
    {
        get => _type;
        set => _type = (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    [Range(1, int.MaxValue, ErrorMessage = MovementValidationMessages.ProductRequired)]
    [Display(Name = "Producto")]
    public int ProductId { get; set; }

    // Signo y ≠ 0: Application (R3–R4). int no usa [Required] (0 sería inválido a la fuerza).
    [Display(Name = "Cantidad")]
    public int Quantity { get; set; }

    [MaxLength(250, ErrorMessage = MovementValidationMessages.ReasonMaxLength)]
    [Display(Name = "Motivo")]
    public string? Reason
    {
        get => _reason;
        set => _reason = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
