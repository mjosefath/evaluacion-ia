using Inventario.Contracts.Auth;

namespace Inventario.ApiClient.Auth;

public interface IInventarioAuthApi
{
    Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthTokenResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<MeResponse> MeAsync(CancellationToken cancellationToken = default);
}
