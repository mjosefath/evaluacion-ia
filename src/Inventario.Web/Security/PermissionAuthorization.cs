using Inventario.Contracts.Security;

namespace Inventario.Web.Security;

public static class PermissionAuthorization
{
    public static IServiceCollection AddInventarioPermissionPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var code in PermissionCodes.All)
            {
                options.AddPolicy(
                    code,
                    policy => policy.RequireClaim(PermissionCodes.PermissionClaimType, code));
            }
        });

        return services;
    }
}
