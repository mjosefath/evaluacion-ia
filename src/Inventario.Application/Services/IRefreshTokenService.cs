using Inventario.Data.Identity;

namespace Inventario.Application.Services;

public interface IRefreshTokenService
{
    string HashToken(string plainToken);

    Task<(string PlainToken, RefreshToken Entity)> IssueAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<RefreshToken?> FindActiveByPlainTokenAsync(
        string plainToken,
        CancellationToken cancellationToken = default);

    Task<(string PlainToken, RefreshToken Entity)?> RotateAsync(
        RefreshToken current,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        string userId,
        string plainToken,
        CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default);
}
