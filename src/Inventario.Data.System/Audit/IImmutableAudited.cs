namespace Inventario.Data.System.Audit;

/// <summary>
/// Columnas de auditoría de tablas inmutables (solo creación).
/// </summary>
public interface IImmutableAudited
{
    DateTime CreatedAtUtc { get; set; }

    int? CreatedBy { get; set; }
}
