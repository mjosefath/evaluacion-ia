using System.Security.Claims;
using Inventario.Application.Auth;
using Inventario.Application.Services;
using Inventario.Contracts.Security;
using Inventario.Data.System;
using Microsoft.AspNetCore.Http;

namespace Inventario.Application.Security;

public sealed class CurrentUser(
    IHttpContextAccessor httpContextAccessor,
    IUserProfileService userProfiles) : ICurrentUser
{
    private int? _resolvedProfileId;
    private bool _resolved;

    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    public string? UserId
    {
        get
        {
            var principal = Principal;
            if (principal is null)
            {
                return null;
            }

            return principal.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? principal.FindFirstValue("sub");
        }
    }

    public int? UserProfileId
    {
        get
        {
            if (_resolved)
            {
                return _resolvedProfileId;
            }

            return ReadProfileIdClaim();
        }
    }

    public async Task<int?> ResolveUserProfileIdAsync(CancellationToken cancellationToken = default)
    {
        if (_resolved)
        {
            return _resolvedProfileId;
        }

        var fromClaim = ReadProfileIdClaim();
        if (fromClaim is not null)
        {
            _resolvedProfileId = fromClaim;
            _resolved = true;
            return _resolvedProfileId;
        }

        var identityUserId = UserId;
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            _resolved = true;
            _resolvedProfileId = null;
            return null;
        }

        var profile = await userProfiles.FindByIdentityUserIdAsync(identityUserId, cancellationToken);
        _resolvedProfileId = profile is not null && profile.StatusId == RecordStatusIds.Active
            ? profile.Id
            : null;
        _resolved = true;
        return _resolvedProfileId;
    }

    public bool HasPermission(string permissionCode) =>
        !string.IsNullOrWhiteSpace(permissionCode)
        && Principal?.HasClaim(PermissionCodes.PermissionClaimType, permissionCode) == true;

    private int? ReadProfileIdClaim()
    {
        var raw = Principal?.FindFirstValue(AuthClaimTypes.UserProfileId);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return int.TryParse(raw, out var id) && id > 0 ? id : null;
    }
}
