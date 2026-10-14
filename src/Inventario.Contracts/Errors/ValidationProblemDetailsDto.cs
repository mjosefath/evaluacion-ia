namespace Inventario.Contracts.Errors;

/// <summary>
/// Forma esperada de 400 con errors por campo (extensión habitual de ProblemDetails / RFC 7807).
/// ASP.NET puede emitir esta forma; el ApiClient/Web la deserializa sin referenciar MVC.
/// </summary>
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
