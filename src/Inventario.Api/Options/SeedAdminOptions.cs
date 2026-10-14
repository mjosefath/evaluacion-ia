namespace Inventario.Api.Options;

/// <summary>
/// Seed:Admin. Password solo user-secrets/env (R4/R10); no va en appsettings del repo.
/// </summary>
public sealed class SeedAdminOptions
{
    public const string SectionName = "Seed:Admin";

    public string UserName { get; set; } = "admin";

    public string DisplayName { get; set; } = "Administrador";

    /// <summary>Solo secret/env. Nunca en archivos commiteables.</summary>
    public string Password { get; set; } = string.Empty;
}
