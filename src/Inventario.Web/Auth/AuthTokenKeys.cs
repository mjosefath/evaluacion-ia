namespace Inventario.Web.Auth;

internal static class AuthTokenKeys
{
    public const string ItemsCache = "Inventario.AuthTokens";
    public const string AccessToken = "access_token";
    public const string RefreshToken = "refresh_token";
    public const string AccessExpires = "access_expires_utc";
    public const string RefreshExpires = "refresh_expires_utc";
    public const string PermissionClaim = "permission";
}
