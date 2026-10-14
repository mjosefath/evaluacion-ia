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
    public async Task<AuthResult<AuthTokenResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var fieldErrors = ValidateLogin(request);
        if (fieldErrors is not null)
        {
            return AuthResult<AuthTokenResponse>.Failure(
                AuthErrorCode.Validation,
                "Datos de entrada inválidos.",
                fieldErrors);
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

            return AuthResult<AuthTokenResponse>.Failure(
                AuthErrorCode.InvalidCredentials,
                "Credenciales inválidas.");
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

            return AuthResult<AuthTokenResponse>.Failure(
                AuthErrorCode.Lockout,
                "Cuenta bloqueada temporalmente.");
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

            return AuthResult<AuthTokenResponse>.Failure(
                AuthErrorCode.InvalidCredentials,
                "Credenciales inválidas.");
        }

        var profile = await userProfiles.GetOrCreateAsync(user.Id, user.UserName ?? userName, cancellationToken);
        if (!profile.IsActive)
        {
            return AuthResult<AuthTokenResponse>.Failure(
                AuthErrorCode.InactiveProfile,
                "Perfil inactivo.");
        }

        var tokens = await IssueTokensAsync(user, cancellationToken);

        await accessLogs.WriteAsync(
            AccessLogEventTypes.LoginSuccess,
            user.Id,
            userName,
            ipAddress,
            userAgent,
            cancellationToken: cancellationToken);

        return AuthResult<AuthTokenResponse>.Success(tokens);
    }

    public async Task<AuthResult<AuthTokenResponse>> RefreshAsync(
        RefreshRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return AuthResult<AuthTokenResponse>.Failure(
                AuthErrorCode.Validation,
                "Datos de entrada inválidos.",
                new Dictionary<string, string[]>
                {
                    ["refreshToken"] = ["El refresh token es obligatorio."]
                });
        }

        var current = await refreshTokens.FindActiveByPlainTokenAsync(request.RefreshToken, cancellationToken);
        if (current is null)
        {
            return AuthResult<AuthTokenResponse>.Failure(
                AuthErrorCode.InvalidRefreshToken,
                "Refresh token inválido o vencido.");
        }

        var user = await userManager.FindByIdAsync(current.UserId);
        if (user is null)
        {
            return AuthResult<AuthTokenResponse>.Failure(
                AuthErrorCode.Unauthorized,
                "Usuario no encontrado.");
        }

        var profile = await userProfiles.GetOrCreateAsync(
            user.Id,
            user.UserName ?? user.Id,
            cancellationToken);

        if (!profile.IsActive)
        {
            return AuthResult<AuthTokenResponse>.Failure(
                AuthErrorCode.InactiveProfile,
                "Perfil inactivo.");
        }

        var rotated = await refreshTokens.RotateAsync(current, cancellationToken);
        if (rotated is null)
        {
            return AuthResult<AuthTokenResponse>.Failure(
                AuthErrorCode.InvalidRefreshToken,
                "Refresh token inválido o vencido.");
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

        return AuthResult<AuthTokenResponse>.Success(
            new AuthTokenResponse(
                accessToken,
                rotated.Value.PlainToken,
                accessExpires,
                new DateTimeOffset(rotated.Value.Entity.ExpiresAtUtc, TimeSpan.Zero)));
    }

    public async Task<AuthResult<object?>> LogoutAsync(
        string userId,
        LogoutRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return AuthResult<object?>.Failure(AuthErrorCode.Unauthorized, "No autenticado.");
        }

        await refreshTokens.RevokeAsync(userId, request.RefreshToken, cancellationToken);

        await accessLogs.WriteAsync(
            AccessLogEventTypes.Logout,
            userId,
            userNameAttempted: null,
            ipAddress,
            userAgent,
            cancellationToken: cancellationToken);

        return AuthResult<object?>.Success(null);
    }

    public async Task<AuthResult<object?>> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var fieldErrors = ValidateChangePassword(request);
        if (fieldErrors is not null)
        {
            return AuthResult<object?>.Failure(
                AuthErrorCode.Validation,
                "Datos de entrada inválidos.",
                fieldErrors);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AuthResult<object?>.Failure(AuthErrorCode.NotFound, "Usuario no encontrado.");
        }

        var change = await userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!change.Succeeded)
        {
            var errors = change.Errors
                .GroupBy(e => string.IsNullOrWhiteSpace(e.Code) ? "password" : e.Code)
                .ToDictionary(
                    g => MapPasswordField(g.Key),
                    g => g.Select(e => e.Description).ToArray());

            return AuthResult<object?>.Failure(
                AuthErrorCode.PasswordChangeFailed,
                "No se pudo cambiar la contraseña.",
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

        return AuthResult<object?>.Success(null);
    }

    public async Task<AuthResult<MeResponse>> MeAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return AuthResult<MeResponse>.Failure(AuthErrorCode.Unauthorized, "No autenticado.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AuthResult<MeResponse>.Failure(AuthErrorCode.NotFound, "Usuario no encontrado.");
        }

        var profile = await userProfiles.GetOrCreateAsync(
            user.Id,
            user.UserName ?? user.Id,
            cancellationToken);

        if (!profile.IsActive)
        {
            return AuthResult<MeResponse>.Failure(
                AuthErrorCode.InactiveProfile,
                "Perfil inactivo.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var permissionCodes = await permissions.GetPermissionCodesForRolesAsync(roles, cancellationToken);

        return AuthResult<MeResponse>.Success(
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
            errors["userName"] = ["El usuario es obligatorio."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = ["La contraseña es obligatoria."];
        }

        return errors.Count == 0 ? null : errors;
    }

    private static IReadOnlyDictionary<string, string[]>? ValidateChangePassword(ChangePasswordRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            errors["currentPassword"] = ["La contraseña actual es obligatoria."];
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            errors["newPassword"] = ["La contraseña nueva es obligatoria."];
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
