namespace Inventario.Application.Results;

/// <summary>
/// Fuente única de mensajes de servidor (M1–M2). Ampliar por módulo; la Web no reescribe.
/// </summary>
public static class MessageCatalog
{
    public const string ValidationFailed = "Revisa los datos del formulario e inténtalo de nuevo.";
    public const string Unexpected = "Algo salió mal. Inténtalo de nuevo. Si sigue fallando, usa el código de referencia.";
    public const string NotFound = "No encontramos ese registro. Actualiza la lista e inténtalo de nuevo.";
    public const string Forbidden = "No tienes permiso para esta acción.";
    public const string ConflictDuplicate = "Ya existe un registro con ese valor. Usa otro distinto.";
    public const string ConflictConcurrency = "Alguien más cambió este registro. Recarga e inténtalo de nuevo.";

    public const string AuthInvalidCredentials = "Usuario o contraseña incorrectos. Inténtalo de nuevo.";
    public const string AuthSessionExpired = "Tu sesión ha vencido. Inicia sesión de nuevo.";
    public const string AuthLocked = "Cuenta bloqueada temporalmente por intentos fallidos. Espera e inténtalo más tarde.";
    public const string AuthInactiveProfile = "Tu perfil está inactivo. Contacta al administrador.";
    public const string AuthUnauthorized = "No estás autenticado. Inicia sesión de nuevo.";
    public const string AuthPasswordChangeFailed = "No se pudo cambiar la contraseña. Revisa los datos e inténtalo de nuevo.";

    public static string ForCode(string code, string? rule = null) =>
        code switch
        {
            ErrorCodes.Validation => ValidationFailed,
            ErrorCodes.NotFound => NotFound,
            ErrorCodes.Forbidden => Forbidden,
            ErrorCodes.ConflictDuplicate => ConflictDuplicate,
            ErrorCodes.ConflictConcurrency => ConflictConcurrency,
            ErrorCodes.AuthInvalidCredentials => AuthInvalidCredentials,
            ErrorCodes.AuthSessionExpired => AuthSessionExpired,
            ErrorCodes.AuthLocked => AuthLocked,
            ErrorCodes.AuthInactiveProfile => AuthInactiveProfile,
            ErrorCodes.Unexpected => Unexpected,
            ErrorCodes.BusinessRule => rule switch
            {
                BusinessRuleCodes.StockInsufficient =>
                    "Existencia insuficiente. Reduce la cantidad e inténtalo de nuevo.",
                BusinessRuleCodes.ProductInactive =>
                    "El producto está inactivo. Actívalo o elige otro.",
                BusinessRuleCodes.ProductHasStock =>
                    "El producto aún tiene existencia. Ajústala a cero antes de desactivarlo.",
                BusinessRuleCodes.AdjustmentReasonRequired =>
                    "Indica el motivo del ajuste e inténtalo de nuevo.",
                _ => "No se pudo completar la operación por una regla de negocio."
            },
            _ => Unexpected
        };
}
