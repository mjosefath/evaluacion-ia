using Inventario.Application.Auth;
using Inventario.Application.Options;
using Inventario.Application.Security;
using Inventario.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Inventario.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra casos de uso y servicios de Application. Sin connection strings;
    /// JwtOptions y DbContext/Identity los configura el host (Api). Requiere IHttpContextAccessor en el host.
    /// </summary>
    public static IServiceCollection AddInventarioApplication(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>();

        services.AddScoped<IAccessLogWriter, AccessLogWriter>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IPermissionLookup, PermissionLookup>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}
