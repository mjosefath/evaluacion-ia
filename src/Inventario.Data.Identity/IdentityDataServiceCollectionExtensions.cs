using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventario.Data.Identity;

public static class IdentityDataServiceCollectionExtensions
{
    /// <summary>
    /// Registra <see cref="InventarioIdentityDbContext"/>. La cadena la aporta el host (user-secrets/env).
    /// </summary>
    public static IServiceCollection AddInventarioIdentityData(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddDbContext<InventarioIdentityDbContext>(configureOptions);
        return services;
    }
}
