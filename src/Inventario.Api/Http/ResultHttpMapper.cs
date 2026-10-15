using Inventario.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Http;

/// <summary>
/// Mapeador único Result → HTTP + ApiProblem (análisis 03, E5).
/// </summary>
internal static class ResultHttpMapper
{
    public static IActionResult ToActionResult(ControllerBase controller, Result result)
    {
        if (result.IsSuccess)
        {
            return controller.NoContent();
        }

        return Failure(controller, result);
    }

    public static IActionResult ToActionResult<T>(ControllerBase controller, Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Value is null
                ? controller.NoContent()
                : controller.Ok(result.Value);
        }

        return Failure(controller, result);
    }

    private static IActionResult Failure(ControllerBase controller, Result result)
    {
        var problem = ApiProblemFactory.FromResult(controller.HttpContext, result);
        return new ObjectResult(problem) { StatusCode = problem.Status };
    }
}
