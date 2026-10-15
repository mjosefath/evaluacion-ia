using System.Data;
using System.Data.Common;
using Inventario.Application.Common;
using Inventario.Application.Data;
using Inventario.Application.Results;
using Inventario.Contracts.Common;
using Inventario.Contracts.Products;
using Inventario.Data.System;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Application.Products;

public sealed class ProductQueryService(InventarioSystemDbContext systemDb) : IProductQueryService
{
    public async Task<Result<PagedResult<ProductListItemDto>>> ListAsync(
        ProductListQuery query,
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
            StoredProcedureExecutor.In(
                "@CategoryId",
                query.CategoryId.HasValue ? query.CategoryId.Value : DBNull.Value,
                SqlDbType.Int),
            StoredProcedureExecutor.In(
                "@StatusId",
                query.StatusId.HasValue ? query.StatusId.Value : DBNull.Value,
                SqlDbType.TinyInt)
        };

        var (items, totalRows) = await StoredProcedureExecutor.QueryPagedAsync(
            systemDb,
            "usp_Products_List",
            parameters,
            MapListItem,
            cancellationToken);

        return Result<PagedResult<ProductListItemDto>>.Success(
            new PagedResult<ProductListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalRows = totalRows
            });
    }

    public async Task<Result<ProductDetailDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id < 1)
        {
            return Result<ProductDetailDto>.Failure(ErrorCodes.NotFound, MessageCatalog.NotFound);
        }

        var row = await (
            from p in systemDb.Products.AsNoTracking()
            join c in systemDb.Categories.AsNoTracking() on p.CategoryId equals c.Id
            join rs in systemDb.RecordStatuses.AsNoTracking() on p.StatusId equals rs.Id
            join stock in systemDb.ProductStocks.AsNoTracking() on p.Id equals stock.ProductId
            where p.Id == id
            select new
            {
                p.Id,
                p.Code,
                p.Name,
                p.CategoryId,
                CategoryName = c.Name,
                p.UnitPrice,
                p.MinStock,
                stock.Stock,
                p.StatusId,
                StatusName = rs.Name,
                p.RowVersion
            }).FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return Result<ProductDetailDto>.Failure(ErrorCodes.NotFound, MessageCatalog.NotFound);
        }

        var stockStatus = row.Stock <= row.MinStock ? "BAJO" : "NORMAL";

        return Result<ProductDetailDto>.Success(
            new ProductDetailDto(
                row.Id,
                row.Code,
                row.Name,
                row.CategoryId,
                row.CategoryName,
                row.UnitPrice,
                row.MinStock,
                row.Stock,
                stockStatus,
                row.StatusId,
                row.StatusName,
                Convert.ToBase64String(row.RowVersion)));
    }

    private static ProductListItemDto MapListItem(DbDataReader r)
    {
        var rowVersion = (byte[])r["RowVersion"];
        return new ProductListItemDto(
            r.GetInt32(r.GetOrdinal("Id")),
            r.GetString(r.GetOrdinal("Code")),
            r.GetString(r.GetOrdinal("Name")),
            r.GetInt32(r.GetOrdinal("CategoryId")),
            r.GetString(r.GetOrdinal("CategoryName")),
            r.GetDecimal(r.GetOrdinal("UnitPrice")),
            r.GetInt32(r.GetOrdinal("MinStock")),
            r.GetInt32(r.GetOrdinal("Stock")),
            r.GetString(r.GetOrdinal("StockStatus")),
            r.GetByte(r.GetOrdinal("StatusId")),
            r.GetString(r.GetOrdinal("StatusName")),
            Convert.ToBase64String(rowVersion));
    }

    private static object? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
