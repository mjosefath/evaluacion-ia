using Inventario.Application.Results;

namespace Inventario.Application.Auth;

/// <summary>
/// Puente temporal AuthErrorCode ↔ códigos E2 hasta migrar AuthService (T03-06).
/// </summary>
public static class AuthErrorCodeMapper
{
    public static string ToErrorCode(AuthErrorCode code) => code switch
    {
        AuthErrorCode.InvalidCredentials => ErrorCodes.AuthInvalidCredentials,
        AuthErrorCode.Lockout => ErrorCodes.AuthLocked,
        AuthErrorCode.InactiveProfile => ErrorCodes.AuthInactiveProfile,
        AuthErrorCode.Validation or AuthErrorCode.PasswordChangeFailed => ErrorCodes.Validation,
        AuthErrorCode.Unauthorized or AuthErrorCode.InvalidRefreshToken => ErrorCodes.AuthSessionExpired,
        AuthErrorCode.NotFound => ErrorCodes.NotFound,
        AuthErrorCode.None => string.Empty,
        _ => ErrorCodes.Unexpected
    };

    /// <summary>
    /// Invierte a AuthErrorCode para AuthResultHttpMapper (hasta T03-07).
    /// session_expired → Unauthorized (mismo HTTP 401 que InvalidRefreshToken).
    /// </summary>
    public static AuthErrorCode FromErrorCode(string? code) => code switch
    {
        ErrorCodes.AuthInvalidCredentials => AuthErrorCode.InvalidCredentials,
        ErrorCodes.AuthLocked => AuthErrorCode.Lockout,
        ErrorCodes.AuthInactiveProfile => AuthErrorCode.InactiveProfile,
        ErrorCodes.Validation => AuthErrorCode.Validation,
        ErrorCodes.AuthSessionExpired => AuthErrorCode.InvalidRefreshToken,
        ErrorCodes.NotFound => AuthErrorCode.NotFound,
        ErrorCodes.Forbidden => AuthErrorCode.Unauthorized,
        _ => AuthErrorCode.None
    };

    public static string DefaultMessage(AuthErrorCode code) =>
        MessageCatalog.ForCode(ToErrorCode(code));
}
