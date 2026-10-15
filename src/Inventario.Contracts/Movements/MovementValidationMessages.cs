namespace Inventario.Contracts.Movements;

/// <summary>Mensajes de formato de movimientos (V2).</summary>
public static class MovementValidationMessages
{
    public const string TypeRequired = "El tipo de movimiento es obligatorio.";
    public const string TypeMaxLength = "El tipo no puede superar {1} caracteres.";
    public const string ProductRequired = "El producto es obligatorio.";
    public const string QuantityRequired = "La cantidad es obligatoria.";
    public const string ReasonMaxLength = "El motivo no puede superar {1} caracteres.";
}
