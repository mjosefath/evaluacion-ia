using Inventario.Data.System.Audit;

namespace Inventario.Application.Audit;

/// <summary>
/// Aplica Created*/Updated* en el servidor (E5). La Web no envía estos campos.
/// </summary>
public static class AuditStamp
{
    public static void ApplyCreate(IMutableAudited entity, int userProfileId, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        entity.CreatedAtUtc = now;
        entity.CreatedBy = userProfileId;
    }

    public static void ApplyCreate(IImmutableAudited entity, int userProfileId, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        entity.CreatedAtUtc = now;
        entity.CreatedBy = userProfileId;
    }

    public static void ApplyUpdate(IMutableAudited entity, int userProfileId, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        entity.UpdatedAtUtc = now;
        entity.UpdatedBy = userProfileId;
    }
}
