using Inventario.ApiClient.Auth;
using Inventario.ApiClient.Catalogs;
using Inventario.ApiClient.Movements;
using Inventario.ApiClient.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Inventario.ApiClient;

public static class DependencyInjection
{
    /// <summary>
    /// Registra APIs tipadas + handler de refresh. La Web debe registrar IAuthTokenStore.
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

        services.AddHttpClient<IInventarioCatalogsApi, InventarioCatalogsApi>(client =>
            {
                client.BaseAddress = baseAddress;
            })
            .AddHttpMessageHandler<AuthRefreshHandler>();

        services.AddHttpClient<IInventarioProductsApi, InventarioProductsApi>(client =>
            {
                client.BaseAddress = baseAddress;
            })
            .AddHttpMessageHandler<AuthRefreshHandler>();

        services.AddHttpClient<IInventarioMovementsApi, InventarioMovementsApi>(client =>
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
