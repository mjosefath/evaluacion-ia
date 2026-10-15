using Inventario.Contracts.Security;

namespace Inventario.Api.Security;

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

            options.AddPolicy(
                PermissionCodes.MovimientosAny,
                policy => policy.RequireAssertion(ctx =>
                    PermissionCodes.MovimientosAll.Any(code =>
                        ctx.User.HasClaim(PermissionCodes.PermissionClaimType, code))));
        });

        return services;
    }
}
