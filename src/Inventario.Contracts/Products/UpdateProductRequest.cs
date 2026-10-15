using System.ComponentModel.DataAnnotations;

namespace Inventario.Contracts.Products;

/// <summary>
/// PUT /api/products/{id}.
/// <see cref="RowVersion"/> es Base64 del token de concurrencia (no editar código).
/// </summary>
public sealed class UpdateProductRequest
{
    private string _name = string.Empty;
    private string _rowVersion = string.Empty;

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

    /// <summary>RowVersion en Base64 (E6).</summary>
    [Required(ErrorMessage = ProductValidationMessages.RowVersionRequired)]
    public string RowVersion
    {
        get => _rowVersion;
        set => _rowVersion = (value ?? string.Empty).Trim();
    }
}
