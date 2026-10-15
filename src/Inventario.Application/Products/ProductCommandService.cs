using System.Data.Common;
using Inventario.Application.Audit;
using Inventario.Application.Results;
using Inventario.Application.Security;
using Inventario.Contracts.Products;
using Inventario.Data.System;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Application.Products;

public sealed class ProductCommandService(
    InventarioSystemDbContext systemDb,
    ICurrentUser currentUser,
    IProductQueryService productQueries) : IProductCommandService
{
    private const string ConcurrencyMessage =
        "El producto cambió mientras lo editabas. Recarga para ver los cambios.";

    private const string DuplicateCodeMessage =
        "Ya existe un producto con ese código. Usa otro distinto.";

    public async Task<Result<ProductDetailDto>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentUser.ResolveUserProfileIdAsync(cancellationToken);
        if (profileId is null)
        {
            return Result<ProductDetailDto>.Failure(ErrorCodes.Forbidden, MessageCatalog.Forbidden);
        }

        var code = NormalizeCode(request.Code);
        if (string.IsNullOrEmpty(code))
        {
            return Result<ProductDetailDto>.Validation(
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["code"] = [ProductValidationMessages.CodeRequired]
                });
        }

        var categoryError = await ValidateActiveCategoryAsync(request.CategoryId, cancellationToken);
        if (categoryError is not null)
        {
            return categoryError;
        }

        if (await CodeExistsAsync(code, excludeId: null, cancellationToken))
        {
            return DuplicateCode();
        }

        var entity = new Product
        {
            Code = code,
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            UnitPrice = RoundMoney(request.UnitPrice),
            MinStock = request.MinStock,
            StatusId = RecordStatusIds.Active
        };
        AuditStamp.ApplyCreate(entity, profileId.Value);

        systemDb.Products.Add(entity);

        try
        {
            await systemDb.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return DuplicateCode();
        }

        return await productQueries.GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<Result<ProductDetailDto>> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id < 1)
        {
            return Result<ProductDetailDto>.Failure(ErrorCodes.NotFound, MessageCatalog.NotFound);
        }

        var profileId = await currentUser.ResolveUserProfileIdAsync(cancellationToken);
        if (profileId is null)
        {
            return Result<ProductDetailDto>.Failure(ErrorCodes.Forbidden, MessageCatalog.Forbidden);
        }

        byte[] rowVersion;
        try
        {
            rowVersion = Convert.FromBase64String(request.RowVersion);
        }
        catch (FormatException)
        {
            return Result<ProductDetailDto>.Validation(
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["rowVersion"] = [ProductValidationMessages.RowVersionRequired]
                });
        }

        var categoryError = await ValidateActiveCategoryAsync(request.CategoryId, cancellationToken);
        if (categoryError is not null)
        {
            return categoryError;
        }

        var entity = await systemDb.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result<ProductDetailDto>.Failure(ErrorCodes.NotFound, MessageCatalog.NotFound);
        }

        systemDb.Entry(entity).Property(p => p.RowVersion).OriginalValue = rowVersion;

        entity.Name = request.Name.Trim();
        entity.CategoryId = request.CategoryId;
        entity.UnitPrice = RoundMoney(request.UnitPrice);
        entity.MinStock = request.MinStock;
        AuditStamp.ApplyUpdate(entity, profileId.Value);

        try
        {
            await systemDb.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<ProductDetailDto>.Failure(
                ErrorCodes.ConflictConcurrency,
                ConcurrencyMessage);
        }

        return await productQueries.GetByIdAsync(entity.Id, cancellationToken);
    }

    private async Task<Result<ProductDetailDto>?> ValidateActiveCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        var ok = await systemDb.Categories.AsNoTracking()
            .AnyAsync(
                c => c.Id == categoryId && c.StatusId == RecordStatusIds.Active,
                cancellationToken);

        if (ok)
        {
            return null;
        }

        return Result<ProductDetailDto>.Validation(
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["categoryId"] = [ProductValidationMessages.CategoryRequired]
            });
    }

    private Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken cancellationToken) =>
        systemDb.Products.AsNoTracking()
            .AnyAsync(
                p => p.Code == code && (excludeId == null || p.Id != excludeId),
                cancellationToken);

    private static string NormalizeCode(string? code) =>
        (code ?? string.Empty).Trim().ToUpperInvariant();

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static Result<ProductDetailDto> DuplicateCode() =>
        Result<ProductDetailDto>.Failure(
            ErrorCodes.ConflictDuplicate,
            DuplicateCodeMessage,
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["code"] = [DuplicateCodeMessage]
            });

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            if (e is SqlException sql && (sql.Number is 2601 or 2627))
            {
                return true;
            }

            if (e is DbException dbEx
                && dbEx.Message.Contains("UX_Products_Code", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
