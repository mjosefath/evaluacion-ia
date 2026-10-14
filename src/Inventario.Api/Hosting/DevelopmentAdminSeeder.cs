using Inventario.Api.Options;
using Inventario.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Inventario.Api.Hosting;

/// <summary>
/// Asegura el admin de Development (R10). Idempotente: no resetea password si el usuario ya existe.
/// user-secrets (sin valores aquí): Seed:Admin:Password, ConnectionStrings:*, Jwt:SigningKey.
/// </summary>
public static class DevelopmentAdminSeeder
{
    public const string AdministradorRoleName = "Administrador";

    public static async Task EnsureAdminAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentAdminSeeder");
        var options = sp.GetRequiredService<IOptions<SeedAdminOptions>>().Value;
        var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var profiles = sp.GetRequiredService<IUserProfileService>();

        var userName = (options.UserName ?? string.Empty).Trim();
        var displayName = string.IsNullOrWhiteSpace(options.DisplayName)
            ? userName
            : options.DisplayName.Trim();

        if (string.IsNullOrWhiteSpace(userName))
        {
            logger.LogWarning("Seed admin omitido: Seed:Admin:UserName vacío.");
            return;
        }

        await EnsureRoleAsync(roleManager, logger, cancellationToken);

        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(options.Password))
            {
                logger.LogWarning(
                    "Seed admin omitido: falta Seed:Admin:Password (user-secrets o variable Seed__Admin__Password).");
                return;
            }

            user = new IdentityUser
            {
                UserName = userName,
                EmailConfirmed = false
            };

            var create = await userManager.CreateAsync(user, options.Password);
            if (!create.Succeeded)
            {
                var errors = string.Join("; ", create.Errors.Select(e => e.Description));
                logger.LogError("No se pudo crear el usuario admin de Development: {Errors}", errors);
                return;
            }

            logger.LogInformation("Usuario admin de Development creado: {UserName}", userName);
        }
        else
        {
            // Existe: no tocar password.
            logger.LogInformation("Usuario admin de Development ya existe: {UserName}", userName);
        }

        if (!await userManager.IsInRoleAsync(user, AdministradorRoleName))
        {
            var addRole = await userManager.AddToRoleAsync(user, AdministradorRoleName);
            if (!addRole.Succeeded)
            {
                var errors = string.Join("; ", addRole.Errors.Select(e => e.Description));
                logger.LogError("No se pudo asignar el rol Administrador: {Errors}", errors);
                return;
            }
        }

        // Perfil: DisplayName del seed solo al crear; si ya existe, no se pisa.
        await profiles.GetOrCreateAsync(user.Id, displayName, cancellationToken);

        logger.LogInformation(
            "Admin de Development asegurado (usuario, rol {Role}, perfil).",
            AdministradorRoleName);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole> roleManager,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (await roleManager.RoleExistsAsync(AdministradorRoleName))
        {
            return;
        }

        // El script 002 debería haberlo creado; respaldo idempotente si la BD está incompleta.
        var create = await roleManager.CreateAsync(new IdentityRole(AdministradorRoleName));
        if (!create.Succeeded)
        {
            var errors = string.Join("; ", create.Errors.Select(e => e.Description));
            logger.LogError("No se pudo asegurar el rol Administrador: {Errors}", errors);
            return;
        }

        logger.LogInformation("Rol {Role} creado por seeder (no estaba en Identity).", AdministradorRoleName);
    }
}
