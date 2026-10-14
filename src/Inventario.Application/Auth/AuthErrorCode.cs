namespace Inventario.Application.Auth;

/// <summary>
/// Códigos de fallo para que la Api mapee a HTTP (ADR-002).
/// </summary>
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
