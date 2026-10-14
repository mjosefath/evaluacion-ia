namespace Inventario.Application.Auth;

public static class AccessLogEventTypes
{
    public const string LoginSuccess = "LoginSuccess";
    public const string LoginFailure = "LoginFailure";
    public const string Lockout = "Lockout";
    public const string Logout = "Logout";
    public const string TokenRefresh = "TokenRefresh";
    public const string PasswordChanged = "PasswordChanged";
}
