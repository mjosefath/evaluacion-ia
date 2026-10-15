using System.Data;
using Inventario.Application.Audit;
using Inventario.Application.Results;
using Inventario.Application.Security;
using Inventario.Contracts.Movements;
using Inventario.Contracts.Security;
using Inventario.Data.System;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Application.Movements;

public sealed class MovementCommandService(
    InventarioSystemDbContext systemDb,
    ICurrentUser currentUser) : IMovementCommandService
{
    public const string WarningStockLow = "stock.low";

    public async Task<Result<CreateMovementResponse>> CreateAsync(
        CreateMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentUser.ResolveUserProfileIdAsync(cancellationToken);
        if (profileId is null)
        {
            return Result<CreateMovementResponse>.Failure(ErrorCodes.Forbidden, MessageCatalog.Forbidden);
        }

        var typeCode = (request.Type ?? string.Empty).Trim().ToUpperInvariant();
        var permission = PermissionForType(typeCode);
        if (permission is null)
        {
            return Result<CreateMovementResponse>.Validation(
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["type"] = [MovementValidationMessages.TypeRequired]
                });
        }

        if (!currentUser.HasPermission(permission))
        {
            return Result<CreateMovementResponse>.Failure(ErrorCodes.Forbidden, MessageCatalog.Forbidden);
        }

        var quantityCheck = ValidateQuantity(typeCode, request.Quantity, request.Reason);
        if (quantityCheck is not null)
        {
            return quantityCheck;
        }

        var effect = EffectQuantity(typeCode, request.Quantity);

        await using var tx = await systemDb.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var product = await systemDb.Products
            .FromSqlInterpolated(
                $"""
                 SELECT *
                 FROM [dbo].[Products] WITH (UPDLOCK, ROWLOCK)
                 WHERE [Id] = {request.ProductId}
                 """)
            .AsTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return Result<CreateMovementResponse>.Failure(ErrorCodes.NotFound, MessageCatalog.NotFound);
        }

        if (product.StatusId != RecordStatusIds.Active)
        {
            return Result<CreateMovementResponse>.Failure(
                ErrorCodes.BusinessRule,
                "El producto está inactivo. Actívalo o elige otro.",
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["productId"] = ["El producto está inactivo. Actívalo o elige otro."]
                },
                BusinessRuleCodes.ProductInactive);
        }

        var movementType = await systemDb.MovementTypes.AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Code == typeCode && t.StatusId == RecordStatusIds.Active,
                cancellationToken);

        if (movementType is null)
        {
            return Result<CreateMovementResponse>.Validation(
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["type"] = [MovementValidationMessages.TypeRequired]
                });
        }

        var stockBefore = await systemDb.InventoryMovements
            .Where(m => m.ProductId == product.Id)
            .OrderByDescending(m => m.Id)
            .Select(m => (int?)m.StockAfter)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;

        var stockAfter = stockBefore + effect;
        if (stockAfter < 0)
        {
            var message =
                $"Existencia insuficiente: hay {stockBefore} unidades. Reduce la cantidad.";
            return Result<CreateMovementResponse>.Failure(
                ErrorCodes.BusinessRule,
                message,
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["quantity"] = [message]
                },
                BusinessRuleCodes.StockInsufficient);
        }

        var movement = new InventoryMovement
        {
            ProductId = product.Id,
            MovementTypeId = movementType.Id,
            Quantity = effect,
            StockBefore = stockBefore,
            StockAfter = stockAfter,
            Reason = request.Reason
        };
        AuditStamp.ApplyCreate(movement, profileId.Value);

        systemDb.InventoryMovements.Add(movement);
        await systemDb.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var isLow = stockAfter <= product.MinStock;
        var response = new CreateMovementResponse(
            movement.Id,
            product.Id,
            product.Code,
            product.Name,
            movementType.Code,
            effect,
            stockBefore,
            stockAfter,
            isLow,
            product.MinStock);

        if (!isLow)
        {
            return Result<CreateMovementResponse>.Success(response);
        }

        var warning =
            $"Guardado. {product.Name} quedó en {stockAfter} (mínimo {product.MinStock}).";
        return Result<CreateMovementResponse>.Success(
            response,
            [new ResultWarning(WarningStockLow, warning)]);
    }

    private static string? PermissionForType(string typeCode) =>
        typeCode switch
        {
            MovementTypeCodes.Entrada => PermissionCodes.MovimientosEntrada,
            MovementTypeCodes.Salida => PermissionCodes.MovimientosSalida,
            MovementTypeCodes.Ajuste => PermissionCodes.MovimientosAjuste,
            _ => null
        };

    private static int EffectQuantity(string typeCode, int quantity) =>
        typeCode switch
        {
            MovementTypeCodes.Entrada => quantity,
            MovementTypeCodes.Salida => -quantity,
            MovementTypeCodes.Ajuste => quantity,
            _ => quantity
        };

    private static Result<CreateMovementResponse>? ValidateQuantity(
        string typeCode,
        int quantity,
        string? reason)
    {
        if (typeCode is MovementTypeCodes.Entrada or MovementTypeCodes.Salida)
        {
            if (quantity <= 0)
            {
                return Result<CreateMovementResponse>.Validation(
                    new Dictionary<string, string[]>(StringComparer.Ordinal)
                    {
                        ["quantity"] = ["La cantidad debe ser un entero mayor que 0."]
                    });
            }

            return null;
        }

        if (typeCode == MovementTypeCodes.Ajuste)
        {
            if (quantity == 0)
            {
                return Result<CreateMovementResponse>.Validation(
                    new Dictionary<string, string[]>(StringComparer.Ordinal)
                    {
                        ["quantity"] = ["La variación del ajuste no puede ser 0."]
                    });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return Result<CreateMovementResponse>.Failure(
                    ErrorCodes.BusinessRule,
                    "Indica el motivo del ajuste e inténtalo de nuevo.",
                    new Dictionary<string, string[]>(StringComparer.Ordinal)
                    {
                        ["reason"] = ["Indica el motivo del ajuste e inténtalo de nuevo."]
                    },
                    BusinessRuleCodes.AdjustmentReasonRequired);
            }
        }

        return null;
    }
}
