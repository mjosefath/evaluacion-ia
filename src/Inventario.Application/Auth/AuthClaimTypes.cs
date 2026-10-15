namespace Inventario.Application.Auth;

public static class AuthClaimTypes
{
    public const string Permission = "permission";

    /// <summary>
    /// UserProfiles.Id (int) en el access token. E5: auditoría CreatedBy/UpdatedBy.
    /// </summary>
    public const string UserProfileId = "profile_id";
}
