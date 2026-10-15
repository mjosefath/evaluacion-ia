using System.ComponentModel.DataAnnotations;
using Inventario.Contracts.Products;

namespace Inventario.Web.Models;

/// <summary>Formulario de alta en action-modal (mismas reglas V2 que CreateProductRequest).</summary>
public sealed class ProductCreateFormModel
{
    private string _code = string.Empty;
    private string _name = string.Empty;

    [Required(ErrorMessage = ProductValidationMessages.CodeRequired)]
    [MaxLength(20, ErrorMessage = ProductValidationMessages.CodeMaxLength)]
    [Display(Name = "Código")]
    public string Code
    {
        get => _code;
        set => _code = (value ?? string.Empty).Trim();
    }

    [Required(ErrorMessage = ProductValidationMessages.NameRequired)]
    [MaxLength(150, ErrorMessage = ProductValidationMessages.NameMaxLength)]
    [Display(Name = "Nombre")]
    public string Name
    {
        get => _name;
        set => _name = (value ?? string.Empty).Trim();
    }

    [Range(1, int.MaxValue, ErrorMessage = ProductValidationMessages.CategoryRequired)]
    [Display(Name = "Categoría")]
    public int CategoryId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = ProductValidationMessages.UnitPriceRange)]
    [Display(Name = "Precio unitario")]
    public decimal UnitPrice { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = ProductValidationMessages.MinStockRange)]
    [Display(Name = "Stock mínimo")]
    public int MinStock { get; set; }

    public CreateProductRequest ToRequest() =>
        new()
        {
            Code = Code,
            Name = Name,
            CategoryId = CategoryId,
            UnitPrice = UnitPrice,
            MinStock = MinStock
        };
}
