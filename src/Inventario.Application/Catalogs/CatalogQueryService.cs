using System.Data;
using System.Data.Common;
using Inventario.Application.Common;
using Inventario.Application.Data;
using Inventario.Application.Results;
using Inventario.Contracts.Catalogs;
using Inventario.Contracts.Common;
using Inventario.Data.System;
using Microsoft.Data.SqlClient;

namespace Inventario.Application.Catalogs;

public sealed class CatalogQueryService(InventarioSystemDbContext systemDb) : ICatalogQueryService
{
    public async Task<Result<PagedResult<CategoryListItemDto>>> ListCategoriesAsync(
        CategoryListQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        // null → SQL NULL = todos (FL6 filtros); captura envía statusId=1 (PG9 / SP default).
        var parameters = new[]
        {
            StoredProcedureExecutor.In("@Search", NullIfWhiteSpace(query.Search), SqlDbType.NVarChar),
            StoredProcedureExecutor.In("@Page", page, SqlDbType.Int),
            StoredProcedureExecutor.In("@PageSize", pageSize, SqlDbType.Int),
            StoredProcedureExecutor.In("@SortColumn", NullIfWhiteSpace(query.Sort), SqlDbType.NVarChar),
            StoredProcedureExecutor.In("@SortDirection", NullIfWhiteSpace(query.Dir), SqlDbType.NVarChar),
            StatusIdParameter(query.StatusId)
        };

        var (items, totalRows) = await StoredProcedureExecutor.QueryPagedAsync(
            systemDb,
            "usp_Categories_List",
            parameters,
            MapCategory,
            cancellationToken);

        return Result<PagedResult<CategoryListItemDto>>.Success(
            new PagedResult<CategoryListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalRows = totalRows
            });
    }

    public async Task<Result<PagedResult<MovementTypeListItemDto>>> ListMovementTypesAsync(
        MovementTypeListQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var parameters = new[]
        {
            StoredProcedureExecutor.In("@Search", NullIfWhiteSpace(query.Search), SqlDbType.NVarChar),
            StoredProcedureExecutor.In("@Page", page, SqlDbType.Int),
            StoredProcedureExecutor.In("@PageSize", pageSize, SqlDbType.Int),
            StoredProcedureExecutor.In("@SortColumn", NullIfWhiteSpace(query.Sort), SqlDbType.NVarChar),
            StoredProcedureExecutor.In("@SortDirection", NullIfWhiteSpace(query.Dir), SqlDbType.NVarChar),
            StatusIdParameter(query.StatusId)
        };

        var (items, totalRows) = await StoredProcedureExecutor.QueryPagedAsync(
            systemDb,
            "usp_MovementTypes_List",
            parameters,
            MapMovementType,
            cancellationToken);

        return Result<PagedResult<MovementTypeListItemDto>>.Success(
            new PagedResult<MovementTypeListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalRows = totalRows
            });
    }

    public async Task<Result<PagedResult<RecordStatusListItemDto>>> ListRecordStatusesAsync(
        RecordStatusListQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var parameters = new[]
        {
            StoredProcedureExecutor.In("@Search", NullIfWhiteSpace(query.Search), SqlDbType.NVarChar),
            StoredProcedureExecutor.In("@Page", page, SqlDbType.Int),
            StoredProcedureExecutor.In("@PageSize", pageSize, SqlDbType.Int),
            StoredProcedureExecutor.In("@SortColumn", NullIfWhiteSpace(query.Sort), SqlDbType.NVarChar),
            StoredProcedureExecutor.In("@SortDirection", NullIfWhiteSpace(query.Dir), SqlDbType.NVarChar)
        };

        var (items, totalRows) = await StoredProcedureExecutor.QueryPagedAsync(
            systemDb,
            "usp_RecordStatuses_List",
            parameters,
            MapRecordStatus,
            cancellationToken);

        return Result<PagedResult<RecordStatusListItemDto>>.Success(
            new PagedResult<RecordStatusListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalRows = totalRows
            });
    }

    private static SqlParameter StatusIdParameter(byte? statusId) =>
        StoredProcedureExecutor.In(
            "@StatusId",
            statusId.HasValue ? statusId.Value : DBNull.Value,
            SqlDbType.TinyInt);

    private static CategoryListItemDto MapCategory(DbDataReader r) =>
        new(
            r.GetInt32(r.GetOrdinal("Id")),
            r.GetString(r.GetOrdinal("Name")),
            r.GetByte(r.GetOrdinal("StatusId")),
            r.GetString(r.GetOrdinal("StatusName")));

    private static MovementTypeListItemDto MapMovementType(DbDataReader r) =>
        new(
            r.GetInt32(r.GetOrdinal("Id")),
            r.GetString(r.GetOrdinal("Code")),
            r.GetString(r.GetOrdinal("Name")),
            r.GetByte(r.GetOrdinal("StatusId")),
            r.GetString(r.GetOrdinal("StatusName")));

    private static RecordStatusListItemDto MapRecordStatus(DbDataReader r) =>
        new(
            r.GetByte(r.GetOrdinal("Id")),
            r.GetString(r.GetOrdinal("Code")),
            r.GetString(r.GetOrdinal("Name")));

    private static object? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
