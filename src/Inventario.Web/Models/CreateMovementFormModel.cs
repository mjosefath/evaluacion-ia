using System.ComponentModel.DataAnnotations;
using Inventario.Contracts.Movements;

namespace Inventario.Web.Models;

public sealed class CreateMovementFormModel
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

    [Display(Name = "Cantidad")]
    public int Quantity { get; set; }

    [MaxLength(250, ErrorMessage = MovementValidationMessages.ReasonMaxLength)]
    [Display(Name = "Motivo")]
    public string? Reason
    {
        get => _reason;
        set => _reason = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public CreateMovementRequest ToRequest() =>
        new()
        {
            Type = Type,
            ProductId = ProductId,
            Quantity = Quantity,
            Reason = Reason
        };
}
