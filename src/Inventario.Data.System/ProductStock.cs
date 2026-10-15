namespace Inventario.Data.System;

/// <summary>
/// Fila de <c>vw_ProductStock</c>: existencia actual por producto.
/// </summary>
public sealed class ProductStock
{
    public int ProductId { get; set; }

    public int Stock { get; set; }
}
