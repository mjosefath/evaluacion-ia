using Inventario.Application.Results;
using Microsoft.AspNetCore.Diagnostics;

namespace Inventario.Api.Http;

/// <summary>
/// Excepciones no controladas → ApiProblem unexpected (E7: sin stack/SQL al cliente).
/// </summary>
internal sealed class ApiProblemExceptionHandler(
    ILogger<ApiProblemExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Error no controlado. TraceId={TraceId} Code={Code}",
            ApiProblemFactory.ResolveTraceId(httpContext),
            ErrorCodes.Unexpected);

        var problem = ApiProblemFactory.Unexpected(httpContext);
        httpContext.Response.StatusCode = problem.Status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
