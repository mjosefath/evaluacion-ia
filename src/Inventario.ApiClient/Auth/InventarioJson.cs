using System.Text.Json;

namespace Inventario.ApiClient.Auth;

internal static class InventarioJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
