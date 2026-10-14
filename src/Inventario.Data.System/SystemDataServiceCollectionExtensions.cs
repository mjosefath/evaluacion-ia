using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventario.Data.System;

public static class SystemDataServiceCollectionExtensions
{
    /// <summary>
    /// Registra <see cref="InventarioSystemDbContext"/>. La cadena la aporta el host (user-secrets/env).
    /// </summary>
    public static IServiceCollection AddInventarioSystemData(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddDbContext<InventarioSystemDbContext>(configureOptions);
        return services;
    }
}
