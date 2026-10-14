namespace Inventario.Web.Options;

public sealed class AuthCookieOptions
{
    public const string SectionName = "Auth";

    public string CookieName { get; set; } = "Inventario.Session";

    public int CookieExpireDays { get; set; } = 7;
}
