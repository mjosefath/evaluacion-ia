using Inventario.Api.Http;
using Inventario.Application.Movements;
using Inventario.Application.Results;
using Inventario.Contracts.Common;
using Inventario.Contracts.Movements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/movements")]
public sealed class MovementsController(IMovementCommandService movements) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(OperationResultDto<CreateMovementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CreateMovementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMovementRequest request,
        CancellationToken cancellationToken)
    {
        var result = await movements.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return ResultHttpMapper.ToActionResult(this, result);
        }

        var warnings = (result.Warnings ?? Array.Empty<ResultWarning>())
            .Select(w => new WarningDto(w.Code, w.Message))
            .ToArray();

        // W10: siempre envelope con data + warnings (puede ir vacío).
        return Ok(new OperationResultDto<CreateMovementResponse>
        {
            Data = result.Value!,
            Warnings = warnings
        });
    }
}
