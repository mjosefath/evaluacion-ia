namespace Inventario.Application.Options;

/// <summary>
/// Opciones JWT no secretas por defecto. SigningKey solo desde user-secrets/env (R4).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Inventario.Api";

    public string Audience { get; set; } = "Inventario.Web";

    /// <summary>Secreto; no debe vivir en appsettings del repo.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;
}
