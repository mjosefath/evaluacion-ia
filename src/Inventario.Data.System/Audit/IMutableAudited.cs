namespace Inventario.Data.System.Audit;

/// <summary>
/// Columnas de auditoría de tablas mutables (estándar E5 / ADR-009).
/// </summary>
public interface IMutableAudited
{
    DateTime CreatedAtUtc { get; set; }

    int? CreatedBy { get; set; }

    DateTime? UpdatedAtUtc { get; set; }

    int? UpdatedBy { get; set; }
}
