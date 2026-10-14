using Inventario.Application.Results;
using Inventario.Application.Services;
using Inventario.Contracts.Auth;
using Microsoft.AspNetCore.Identity;

namespace Inventario.Application.Auth;

public sealed class AuthService(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    IUserProfileService userProfiles,
    IPermissionLookup permissions,
    IJwtTokenService jwtTokens,
    IRefreshTokenService refreshTokens,
    IAccessLogWriter accessLogs) : IAuthService
{
    public async Task<Result<AuthTokenResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var fieldErrors = ValidateLogin(request);
        if (fieldErrors is not null)
        {
            return Result<AuthTokenResponse>.Validation(fieldErrors);
        }

        var userName = request.UserName.Trim();
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            await accessLogs.WriteAsync(
                AccessLogEventTypes.LoginFailure,
                identityUserId: null,
                userNameAttempted: userName,
                ipAddress,
                userAgent,
                cancellationToken: cancellationToken);

            return Result<AuthTokenResponse>.Failure(
                ErrorCodes.AuthInvalidCredentials,
                MessageCatalog.AuthInvalidCredentials);
        }

        var signIn = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (signIn.IsLockedOut)
        {
            await accessLogs.WriteAsync(
                AccessLogEventTypes.Lockout,
                user.Id,
                userName,
                ipAddress,
                userAgent,
                cancellationToken: cancellationToken);

            return Result<AuthTokenResponse>.Failure(
                ErrorCodes.AuthLocked,
                MessageCatalog.AuthLocked);
        }

        if (!signIn.Succeeded)
        {
            await accessLogs.WriteAsync(
                AccessLogEventTypes.LoginFailure,
                user.Id,
                userName,
                ipAddress,
                userAgent,
                cancellationToken: cancellationToken);

            return Result<AuthTokenResponse>.Failure(
                ErrorCodes.AuthInvalidCredentials,
                MessageCatalog.AuthInvalidCredentials);
        }

        var profile = await userProfiles.GetOrCreateAsync(user.Id, user.UserName ?? userName, cancellationToken);
        if (!profile.IsActive)
        {
            return Result<AuthTokenResponse>.Failure(
                ErrorCodes.AuthInactiveProfile,
                MessageCatalog.AuthInactiveProfile);
        }

        var tokens = await IssueTokensAsync(user, cancellationToken);

        await accessLogs.WriteAsync(
            AccessLogEventTypes.LoginSuccess,
            user.Id,
            userName,
            ipAddress,
            userAgent,
            cancellationToken: cancellationToken);

        return Result<AuthTokenResponse>.Success(tokens);
    }

    public async Task<Result<AuthTokenResponse>> RefreshAsync(
        RefreshRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result<AuthTokenResponse>.Validation(
                new Dictionary<string, string[]>
                {
                    ["refreshToken"] = [MessageCatalog.FieldRefreshTokenRequired]
                });
        }

        var current = await refreshTokens.FindActiveByPlainTokenAsync(request.RefreshToken, cancellationToken);
        if (current is null)
        {
            return Result<AuthTokenResponse>.Failure(
                ErrorCodes.AuthSessionExpired,
                MessageCatalog.AuthSessionExpired);
        }

        var user = await userManager.FindByIdAsync(current.UserId);
        if (user is null)
        {
            return Result<AuthTokenResponse>.Failure(
                ErrorCodes.AuthSessionExpired,
                MessageCatalog.AuthSessionExpired);
        }

        var profile = await userProfiles.GetOrCreateAsync(
            user.Id,
            user.UserName ?? user.Id,
            cancellationToken);

        if (!profile.IsActive)
        {
            return Result<AuthTokenResponse>.Failure(
                ErrorCodes.AuthInactiveProfile,
                MessageCatalog.AuthInactiveProfile);
        }

        var rotated = await refreshTokens.RotateAsync(current, cancellationToken);
        if (rotated is null)
        {
            return Result<AuthTokenResponse>.Failure(
                ErrorCodes.AuthSessionExpired,
                MessageCatalog.AuthSessionExpired);
        }

        var roles = await userManager.GetRolesAsync(user);
        var permissionCodes = await permissions.GetPermissionCodesForRolesAsync(roles, cancellationToken);
        var (accessToken, accessExpires) = jwtTokens.CreateAccessToken(user, roles, permissionCodes);

        await accessLogs.WriteAsync(
            AccessLogEventTypes.TokenRefresh,
            user.Id,
            user.UserName,
            ipAddress,
            userAgent,
            cancellationToken: cancellationToken);

        return Result<AuthTokenResponse>.Success(
            new AuthTokenResponse(
                accessToken,
                rotated.Value.PlainToken,
                accessExpires,
                new DateTimeOffset(rotated.Value.Entity.ExpiresAtUtc, TimeSpan.Zero)));
    }

    public async Task<Result<object?>> LogoutAsync(
        string userId,
        LogoutRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<object?>.Failure(
                ErrorCodes.AuthSessionExpired,
                MessageCatalog.AuthUnauthorized);
        }

        await refreshTokens.RevokeAsync(userId, request.RefreshToken, cancellationToken);

        await accessLogs.WriteAsync(
            AccessLogEventTypes.Logout,
            userId,
            userNameAttempted: null,
            ipAddress,
            userAgent,
            cancellationToken: cancellationToken);

        return Result<object?>.Success(null);
    }

    public async Task<Result<object?>> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var fieldErrors = ValidateChangePassword(request);
        if (fieldErrors is not null)
        {
            return Result<object?>.Validation(fieldErrors);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result<object?>.Failure(
                ErrorCodes.NotFound,
                MessageCatalog.NotFound);
        }

        var change = await userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!change.Succeeded)
        {
            // Descripciones Identity: se reenvían en FieldErrors (excepción M2 del análisis / T03-06).
            var errors = change.Errors
                .GroupBy(e => string.IsNullOrWhiteSpace(e.Code) ? "password" : e.Code)
                .ToDictionary(
                    g => MapPasswordField(g.Key),
                    g => g.Select(e => e.Description).ToArray());

            return Result<object?>.Failure(
                ErrorCodes.Validation,
                MessageCatalog.AuthPasswordChangeFailed,
                errors);
        }

        await refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);

        await accessLogs.WriteAsync(
            AccessLogEventTypes.PasswordChanged,
            user.Id,
            user.UserName,
            ipAddress,
            userAgent,
            cancellationToken: cancellationToken);

        return Result<object?>.Success(null);
    }

    public async Task<Result<MeResponse>> MeAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<MeResponse>.Failure(
                ErrorCodes.AuthSessionExpired,
                MessageCatalog.AuthUnauthorized);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result<MeResponse>.Failure(
                ErrorCodes.NotFound,
                MessageCatalog.NotFound);
        }

        var profile = await userProfiles.GetOrCreateAsync(
            user.Id,
            user.UserName ?? user.Id,
            cancellationToken);

        if (!profile.IsActive)
        {
            return Result<MeResponse>.Failure(
                ErrorCodes.AuthInactiveProfile,
                MessageCatalog.AuthInactiveProfile);
        }

        var roles = await userManager.GetRolesAsync(user);
        var permissionCodes = await permissions.GetPermissionCodesForRolesAsync(roles, cancellationToken);

        return Result<MeResponse>.Success(
            new MeResponse(profile.DisplayName, roles.ToList(), permissionCodes));
    }

    private async Task<AuthTokenResponse> IssueTokensAsync(
        IdentityUser user,
        CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var permissionCodes = await permissions.GetPermissionCodesForRolesAsync(roles, cancellationToken);
        var (accessToken, accessExpires) = jwtTokens.CreateAccessToken(user, roles, permissionCodes);
        var (refreshPlain, refreshEntity) = await refreshTokens.IssueAsync(user.Id, cancellationToken);

        return new AuthTokenResponse(
            accessToken,
            refreshPlain,
            accessExpires,
            new DateTimeOffset(refreshEntity.ExpiresAtUtc, TimeSpan.Zero));
    }

    private static IReadOnlyDictionary<string, string[]>? ValidateLogin(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            errors["userName"] = [MessageCatalog.FieldUserNameRequired];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = [MessageCatalog.FieldPasswordRequired];
        }

        return errors.Count == 0 ? null : errors;
    }

    private static IReadOnlyDictionary<string, string[]>? ValidateChangePassword(ChangePasswordRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            errors["currentPassword"] = [MessageCatalog.FieldCurrentPasswordRequired];
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            errors["newPassword"] = [MessageCatalog.FieldNewPasswordRequired];
        }

        return errors.Count == 0 ? null : errors;
    }

    private static string MapPasswordField(string identityCode) =>
        identityCode switch
        {
            "PasswordMismatch" => "currentPassword",
            "PasswordTooShort" or "PasswordRequiresNonAlphanumeric" or "PasswordRequiresDigit"
                or "PasswordRequiresLower" or "PasswordRequiresUpper" or "PasswordRequiresUniqueChars"
                => "newPassword",
            _ => "newPassword"
        };
}
