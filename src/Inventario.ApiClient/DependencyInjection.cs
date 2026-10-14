using Inventario.ApiClient.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Inventario.ApiClient;

public static class DependencyInjection
{
    /// <summary>
    /// Registra IInventarioAuthApi + handler de refresh. La Web debe registrar IAuthTokenStore.
    /// </summary>
    public static IServiceCollection AddInventarioApiClient(
        this IServiceCollection services,
        Uri baseAddress)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        services.AddHttpClient(InventarioApiClientNames.Refresh, client =>
        {
            client.BaseAddress = baseAddress;
        });

        services.AddTransient<AuthRefreshHandler>();

        services.AddHttpClient<IInventarioAuthApi, InventarioAuthApi>(client =>
            {
                client.BaseAddress = baseAddress;
            })
            .AddHttpMessageHandler<AuthRefreshHandler>();

        return services;
    }

    public static IServiceCollection AddInventarioApiClient(
        this IServiceCollection services,
        string baseAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseAddress);
        return services.AddInventarioApiClient(new Uri(baseAddress, UriKind.Absolute));
    }
}
