using Inventario.Application.Results;
using Inventario.Contracts.Auth;

namespace Inventario.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthTokenResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<Result<AuthTokenResponse>> RefreshAsync(
        RefreshRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<Result<object?>> LogoutAsync(
        string userId,
        LogoutRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<Result<object?>> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<Result<MeResponse>> MeAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
