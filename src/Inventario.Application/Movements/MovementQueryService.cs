using System.Data;
using System.Data.Common;
using Inventario.Application.Common;
using Inventario.Application.Data;
using Inventario.Application.Results;
using Inventario.Contracts.Common;
using Inventario.Contracts.Movements;
using Inventario.Data.System;
using Microsoft.Data.SqlClient;

namespace Inventario.Application.Movements;

public sealed class MovementQueryService(InventarioSystemDbContext systemDb) : IMovementQueryService
{
    public async Task<Result<PagedResult<MovementListItemDto>>> ListAsync(
        MovementListQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var sort = string.IsNullOrWhiteSpace(query.Sort) ? "date" : query.Sort.Trim();
        var dir = string.IsNullOrWhiteSpace(query.Dir) ? "desc" : query.Dir.Trim();

        var parameters = new[]
        {
            StoredProcedureExecutor.In("@Search", NullIfWhiteSpace(query.Search), SqlDbType.NVarChar),
            StoredProcedureExecutor.In("@Page", page, SqlDbType.Int),
            StoredProcedureExecutor.In("@PageSize", pageSize, SqlDbType.Int),
            StoredProcedureExecutor.In("@SortColumn", sort, SqlDbType.NVarChar),
            StoredProcedureExecutor.In("@SortDirection", dir, SqlDbType.NVarChar),
            StoredProcedureExecutor.In(
                "@MovementTypeId",
                query.TypeId.HasValue ? query.TypeId.Value : DBNull.Value,
                SqlDbType.Int),
            StoredProcedureExecutor.In(
                "@DateFromUtc",
                ToUtcDateTime(query.From),
                SqlDbType.DateTime2),
            StoredProcedureExecutor.In(
                "@DateToUtc",
                ToUtcDateTime(query.To),
                SqlDbType.DateTime2)
        };

        var (items, totalRows) = await StoredProcedureExecutor.QueryPagedAsync(
            systemDb,
            "usp_InventoryMovements_List",
            parameters,
            MapItem,
            cancellationToken);

        return Result<PagedResult<MovementListItemDto>>.Success(
            new PagedResult<MovementListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalRows = totalRows
            });
    }

    private static MovementListItemDto MapItem(DbDataReader r)
    {
        var createdAt = r.GetDateTime(r.GetOrdinal("CreatedAtUtc"));
        var createdAtUtc = new DateTimeOffset(DateTime.SpecifyKind(createdAt, DateTimeKind.Utc));

        var reasonOrd = r.GetOrdinal("Reason");
        var createdByOrd = r.GetOrdinal("CreatedBy");
        var createdByNameOrd = r.GetOrdinal("CreatedByName");

        return new MovementListItemDto(
            r.GetInt64(r.GetOrdinal("Id")),
            createdAtUtc,
            r.GetInt32(r.GetOrdinal("ProductId")),
            r.GetString(r.GetOrdinal("ProductCode")),
            r.GetString(r.GetOrdinal("ProductName")),
            r.GetInt32(r.GetOrdinal("MovementTypeId")),
            r.GetString(r.GetOrdinal("MovementTypeCode")),
            r.GetString(r.GetOrdinal("MovementTypeName")),
            r.GetInt32(r.GetOrdinal("Quantity")),
            r.GetInt32(r.GetOrdinal("StockBefore")),
            r.GetInt32(r.GetOrdinal("StockAfter")),
            r.IsDBNull(reasonOrd) ? null : r.GetString(reasonOrd),
            r.IsDBNull(createdByOrd) ? null : r.GetInt32(createdByOrd),
            r.IsDBNull(createdByNameOrd) ? null : r.GetString(createdByNameOrd));
    }

    private static object ToUtcDateTime(DateTimeOffset? value) =>
        value.HasValue ? value.Value.UtcDateTime : DBNull.Value;

    private static object? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
