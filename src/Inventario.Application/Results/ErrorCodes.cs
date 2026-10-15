namespace Inventario.Application.Results;

/// <summary>
/// Catálogo E2: códigos de texto estables (contrato). No renombrar; solo ampliar.
/// </summary>
public static class ErrorCodes
{
    public const string Validation = "validation";
    public const string BusinessRule = "business.rule";
    public const string AuthInvalidCredentials = "auth.invalid_credentials";
    public const string AuthSessionExpired = "auth.session_expired";
    public const string AuthLocked = "auth.locked";
    public const string AuthInactiveProfile = "auth.inactive_profile";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string ConflictDuplicate = "conflict.duplicate";
    public const string ConflictConcurrency = "conflict.concurrency";
    public const string Unexpected = "unexpected";
}

/// <summary>
/// Subcódigos de <see cref="ErrorCodes.BusinessRule"/> (análisis 02 / 03).
/// </summary>
public static class BusinessRuleCodes
{
    public const string StockInsufficient = "stock.insufficient";
    public const string ProductInactive = "product.inactive";
    public const string ProductHasStock = "product.has_stock";
    public const string AdjustmentReasonRequired = "adjustment.reason_required";
}
