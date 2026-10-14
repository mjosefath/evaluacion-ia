using Inventario.Contracts.Auth;

namespace Inventario.Application.Auth;

public interface IAuthService
{
    Task<AuthResult<AuthTokenResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<AuthResult<AuthTokenResponse>> RefreshAsync(
        RefreshRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<AuthResult<object?>> LogoutAsync(
        string userId,
        LogoutRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<AuthResult<object?>> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<AuthResult<MeResponse>> MeAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
