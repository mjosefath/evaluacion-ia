using Microsoft.AspNetCore.Identity;

namespace Inventario.Application.Services;

public interface IJwtTokenService
{
    (string AccessToken, DateTimeOffset ExpiresAtUtc) CreateAccessToken(
        IdentityUser user,
        int userProfileId,
        IEnumerable<string> roles,
        IEnumerable<string> permissions);
}
