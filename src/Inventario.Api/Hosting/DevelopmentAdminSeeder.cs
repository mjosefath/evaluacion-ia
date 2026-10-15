using Inventario.Api.Options;
using Inventario.Application.Services;
using Inventario.Data.System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Inventario.Api.Hosting;

/// <summary>
/// Asegura el admin de Development (R10). Idempotente: no resetea password si el usuario ya existe.
/// user-secrets (sin valores aquí): Seed:Admin:Password, ConnectionStrings:*, Jwt:SigningKey.
/// </summary>
public static class DevelopmentAdminSeeder
{
    public const string AdministradorRoleName = "Administrador";

    private static readonly string[] SeedProductCodes =
    [
        "ELE-001", "ELE-002", "PAP-001", "PAP-002", "LIM-001"
    ];

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
        var systemDb = sp.GetRequiredService<InventarioSystemDbContext>();

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
        var profile = await profiles.GetOrCreateAsync(user.Id, displayName, cancellationToken);

        await AssignSeedCreatedByAsync(systemDb, profile.Id, logger, cancellationToken);

        logger.LogInformation(
            "Admin de Development asegurado (usuario, rol {Role}, perfil).",
            AdministradorRoleName);
    }

    /// <summary>
    /// Si el seed SQL corrió sin perfil, completa CreatedBy en productos/movimientos de carga inicial.
    /// </summary>
    private static async Task AssignSeedCreatedByAsync(
        InventarioSystemDbContext systemDb,
        int profileId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var productsUpdated = await systemDb.Products
            .Where(p => SeedProductCodes.Contains(p.Code) && p.CreatedBy == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.CreatedBy, profileId),
                cancellationToken);

        var productIds = await systemDb.Products.AsNoTracking()
            .Where(p => SeedProductCodes.Contains(p.Code))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var movementsUpdated = 0;
        if (productIds.Count > 0)
        {
            movementsUpdated = await systemDb.InventoryMovements
                .Where(m =>
                    productIds.Contains(m.ProductId)
                    && m.Reason == "Carga inicial"
                    && m.CreatedBy == null)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(m => m.CreatedBy, profileId),
                    cancellationToken);
        }

        if (productsUpdated > 0 || movementsUpdated > 0)
        {
            logger.LogInformation(
                "Seed inventario: CreatedBy={ProfileId} en {Products} productos y {Movements} movimientos.",
                profileId,
                productsUpdated,
                movementsUpdated);
        }
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
