namespace Inventario.Application.Auth;

/// <summary>
/// Códigos legacy de auth (ADR-002). Preferir <see cref="Results.ErrorCodes"/>.
/// </summary>
[Obsolete("Usar Inventario.Application.Results.ErrorCodes (análisis 03 / E2).")]
public enum AuthErrorCode
{
    None = 0,
    InvalidCredentials,
    Lockout,
    InactiveProfile,
    Validation,
    Unauthorized,
    InvalidRefreshToken,
    PasswordChangeFailed,
    NotFound
}
