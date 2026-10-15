using System.Data;
using System.Data.Common;
using Inventario.Application.Common;
using Inventario.Application.Data;
using Inventario.Application.Results;
using Inventario.Contracts.Common;
using Inventario.Contracts.Reports;
using Inventario.Data.System;

namespace Inventario.Application.Reports;

public sealed class ReportQueryService(InventarioSystemDbContext systemDb) : IReportQueryService
{
    public async Task<Result<ReportPagedResult<StockReportItemDto>>> ListStockAsync(
        StockReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var sort = string.IsNullOrWhiteSpace(query.Sort) ? "code" : query.Sort.Trim();
        var dir = string.IsNullOrWhiteSpace(query.Dir) ? "asc" : query.Dir.Trim();

        var parameters = new[]
        {
            StoredProcedureExecutor.In("@Search", NullIfWhiteSpace(query.Search), SqlDbType.NVarChar),
            StoredProcedureExecutor.In("@Page", page, SqlDbType.Int),
            StoredProcedureExecutor.In("@PageSize", pageSize, SqlDbType.Int),
            StoredProcedureExecutor.In("@SortColumn", sort, SqlDbType.NVarChar),
            StoredProcedureExecutor.In("@SortDirection", dir, SqlDbType.NVarChar),
            StoredProcedureExecutor.In(
                "@CategoryId",
                query.CategoryId.HasValue ? query.CategoryId.Value : DBNull.Value,
                SqlDbType.Int)
        };

        var (items, totalRows, grandTotal) = await StoredProcedureExecutor.QueryReportPagedAsync(
            systemDb,
            "usp_Report_Stock",
            parameters,
            MapItem,
            cancellationToken);

        return Result<ReportPagedResult<StockReportItemDto>>.Success(
            new ReportPagedResult<StockReportItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalRows = totalRows,
                GrandTotal = grandTotal
            });
    }

    private static StockReportItemDto MapItem(DbDataReader r) =>
        new(
            r.GetInt32(r.GetOrdinal("ProductId")),
            r.GetString(r.GetOrdinal("Code")),
            r.GetString(r.GetOrdinal("Name")),
            r.GetInt32(r.GetOrdinal("CategoryId")),
            r.GetString(r.GetOrdinal("CategoryName")),
            r.GetDecimal(r.GetOrdinal("UnitPrice")),
            r.GetInt32(r.GetOrdinal("Stock")),
            r.GetInt32(r.GetOrdinal("MinStock")),
            r.GetString(r.GetOrdinal("StockStatus")),
            r.GetDecimal(r.GetOrdinal("InventoryValue")));

    private static object? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
