using System.Security.Claims;
using Inventario.Contracts.Security;

namespace Inventario.Web.Security;

public static class PermissionClaims
{
    public static bool Has(ClaimsPrincipal? user, string permissionCode) =>
        user?.HasClaim(PermissionCodes.PermissionClaimType, permissionCode) == true;

    public static bool HasAny(ClaimsPrincipal? user, params string[] permissionCodes) =>
        permissionCodes.Any(code => Has(user, code));

    public static bool CanSeeMovimientos(ClaimsPrincipal? user) =>
        HasAny(
            user,
            PermissionCodes.MovimientosEntrada,
            PermissionCodes.MovimientosSalida,
            PermissionCodes.MovimientosAjuste);
}
