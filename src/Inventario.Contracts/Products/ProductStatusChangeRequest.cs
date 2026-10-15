using System.ComponentModel.DataAnnotations;

namespace Inventario.Contracts.Products;

/// <summary>
/// POST /api/products/{id}/deactivate | /activate.
/// <see cref="RowVersion"/> en Base64.
/// </summary>
public sealed class ProductStatusChangeRequest
{
    private string _rowVersion = string.Empty;

    [Required(ErrorMessage = ProductValidationMessages.RowVersionRequired)]
    public string RowVersion
    {
        get => _rowVersion;
        set => _rowVersion = (value ?? string.Empty).Trim();
    }
}
