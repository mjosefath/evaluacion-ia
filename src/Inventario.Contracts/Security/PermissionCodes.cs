namespace Inventario.Contracts.Security;

/// <summary>Códigos de permiso (seed 012 / análisis 02).</summary>
public static class PermissionCodes
{
    public const string ProductosVer = "productos.ver";
    public const string ProductosCrear = "productos.crear";
    public const string ProductosEditar = "productos.editar";
    public const string ProductosActivar = "productos.activar";

    public const string MovimientosEntrada = "movimientos.entrada";
    public const string MovimientosSalida = "movimientos.salida";
    public const string MovimientosAjuste = "movimientos.ajuste";

    public const string HistorialVer = "historial.ver";
    public const string ReporteVer = "reporte.ver";

    public const string PermissionClaimType = "permission";

    public static readonly string[] All =
    [
        ProductosVer,
        ProductosCrear,
        ProductosEditar,
        ProductosActivar,
        MovimientosEntrada,
        MovimientosSalida,
        MovimientosAjuste,
        HistorialVer,
        ReporteVer
    ];
}
