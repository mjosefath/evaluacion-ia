using Inventario.Data.System.Audit;

namespace Inventario.Data.System;

public sealed class Permission : IMutableAudited
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Module { get; set; } = null!;

    public byte StatusId { get; set; } = RecordStatusIds.Active;

    public DateTime CreatedAtUtc { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public int? UpdatedBy { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
