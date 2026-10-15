using System.ComponentModel.DataAnnotations;
using Inventario.Contracts.Products;

namespace Inventario.Web.Models;

/// <summary>Formulario de edición en action-modal (código y existencia solo lectura).</summary>
public sealed class ProductEditFormModel
{
    private string _name = string.Empty;
    private string _rowVersion = string.Empty;

    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public int Stock { get; set; }

    public string StockStatus { get; set; } = string.Empty;

    public string? CategoryName { get; set; }

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

    [Required(ErrorMessage = ProductValidationMessages.RowVersionRequired)]
    public string RowVersion
    {
        get => _rowVersion;
        set => _rowVersion = (value ?? string.Empty).Trim();
    }

    public UpdateProductRequest ToRequest() =>
        new()
        {
            Name = Name,
            CategoryId = CategoryId,
            UnitPrice = UnitPrice,
            MinStock = MinStock,
            RowVersion = RowVersion
        };

    public static ProductEditFormModel FromDetail(ProductDetailDto detail) =>
        new()
        {
            Id = detail.Id,
            Code = detail.Code,
            Name = detail.Name,
            CategoryId = detail.CategoryId,
            CategoryName = detail.CategoryName,
            UnitPrice = detail.UnitPrice,
            MinStock = detail.MinStock,
            Stock = detail.Stock,
            StockStatus = detail.StockStatus,
            RowVersion = detail.RowVersion
        };
}
