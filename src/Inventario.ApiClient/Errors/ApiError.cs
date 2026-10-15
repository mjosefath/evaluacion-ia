using System.Text.Json;
using Inventario.ApiClient.Auth;
using Inventario.Contracts.Errors;

namespace Inventario.ApiClient.Errors;

/// <summary>
/// Error tipado del ApiClient a partir de <see cref="ApiProblem"/> (análisis 03, paso 6 / A5).
/// </summary>
public sealed class ApiError
{
    private const string GenericDetail = "No se pudo completar la operación. Intenta de nuevo.";

    public int Status { get; init; }

    public string? Code { get; init; }

    public string? Title { get; init; }

    public string? Detail { get; init; }

    public string? Rule { get; init; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    public string? TraceId { get; init; }

    public static async Task<ApiError> FromHttpResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return FromResponse((int)response.StatusCode, body);
    }

    /// <summary>
    /// Deserializa <see cref="ApiProblem"/>. Si el cuerpo no parsea, no tumba el proceso:
    /// Code vacío o unexpected + Detail genérico.
    /// </summary>
    public static ApiError FromResponse(int statusCode, string? body)
    {
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var problem = JsonSerializer.Deserialize<ApiProblem>(body, InventarioJson.Options);
                if (problem is not null)
                {
                    IReadOnlyDictionary<string, string[]>? errors = null;
                    if (problem.Errors is { Count: > 0 })
                    {
                        errors = new Dictionary<string, string[]>(problem.Errors, StringComparer.Ordinal);
                    }

                    return new ApiError
                    {
                        Status = problem.Status != 0 ? problem.Status : statusCode,
                        Code = problem.Code,
                        Title = problem.Title,
                        Detail = problem.Detail ?? GenericDetail,
                        Rule = problem.Rule,
                        Errors = errors,
                        TraceId = problem.TraceId
                    };
                }
            }
            catch (JsonException)
            {
                // Cuerpo no es ApiProblem: fallback abajo.
            }
        }

        return new ApiError
        {
            Status = statusCode,
            Code = statusCode >= 500 ? "unexpected" : null,
            Detail = GenericDetail
        };
    }
}
