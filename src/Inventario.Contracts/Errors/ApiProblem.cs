namespace Inventario.Contracts.Errors;

/// <summary>
/// Contrato único de error de la API (análisis 03, E3). Reemplaza <see cref="ValidationProblemDetailsDto"/>.
/// </summary>
public sealed class ApiProblem
{
    public string? Type { get; init; }

    /// <summary>Título corto en español.</summary>
    public string? Title { get; init; }

    public int Status { get; init; }

    /// <summary>Mensaje para el usuario (qué pasó + qué hacer).</summary>
    public string? Detail { get; init; }

    /// <summary>Código estable del catálogo E2 (p. ej. validation, auth.locked).</summary>
    public string? Code { get; init; }

    /// <summary>Subcódigo de business.rule (p. ej. stock.insufficient).</summary>
    public string? Rule { get; init; }

    /// <summary>Errores por campo (camelCase del DTO; llave vacía = general).</summary>
    public IDictionary<string, string[]>? Errors { get; init; }

    /// <summary>Código de referencia W3C / correlación Web–API.</summary>
    public string? TraceId { get; init; }
}
