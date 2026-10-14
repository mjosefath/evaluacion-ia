namespace Inventario.Contracts.Errors;

/// <summary>
/// Obsoleto: usar <see cref="ApiProblem"/> (análisis 03, E3).
/// Se mantiene temporalmente por compatibilidad de deserialización.
/// </summary>
[Obsolete("Usar Inventario.Contracts.Errors.ApiProblem (análisis 03 / E3).")]
public sealed class ValidationProblemDetailsDto
{
    public string? Type { get; init; }

    public string? Title { get; init; }

    public int? Status { get; init; }

    public string? Detail { get; init; }

    public string? Instance { get; init; }

    /// <summary>Clave = nombre de campo; valor = mensajes.</summary>
    public IDictionary<string, string[]>? Errors { get; init; }
}
