namespace Inventario.Contracts.Products;

/// <summary>Mensajes de formato de productos (V2).</summary>
public static class ProductValidationMessages
{
    public const string CodeRequired = "El código es obligatorio.";
    public const string CodeMaxLength = "El código no puede superar {1} caracteres.";
    public const string NameRequired = "El nombre es obligatorio.";
    public const string NameMaxLength = "El nombre no puede superar {1} caracteres.";
    public const string CategoryRequired = "La categoría es obligatoria.";
    public const string UnitPriceRange = "El precio unitario no puede ser negativo.";
    public const string MinStockRange = "El stock mínimo no puede ser negativo.";
    public const string RowVersionRequired = "El token de concurrencia es obligatorio. Recarga e inténtalo de nuevo.";
}
