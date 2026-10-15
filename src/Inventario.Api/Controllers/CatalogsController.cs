using Inventario.Api.Http;
using Inventario.Application.Catalogs;
using Inventario.Contracts.Catalogs;
using Inventario.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Controllers;

/// <summary>
/// Catálogos paginados (análisis 02 / PG8). Autenticado; sin permiso de módulo.
/// </summary>
[ApiController]
[Authorize]
[Route("api")]
public sealed class CatalogsController(ICatalogQueryService catalogs) : ControllerBase
{
    [HttpGet("categories")]
    [ProducesResponseType(typeof(PagedResult<CategoryListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListCategories(
        [FromQuery] CategoryListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await catalogs.ListCategoriesAsync(query, cancellationToken);
        return ResultHttpMapper.ToActionResult(this, result);
    }

    [HttpGet("movement-types")]
    [ProducesResponseType(typeof(PagedResult<MovementTypeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListMovementTypes(
        [FromQuery] MovementTypeListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await catalogs.ListMovementTypesAsync(query, cancellationToken);
        return ResultHttpMapper.ToActionResult(this, result);
    }

    [HttpGet("record-statuses")]
    [ProducesResponseType(typeof(PagedResult<RecordStatusListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListRecordStatuses(
        [FromQuery] RecordStatusListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await catalogs.ListRecordStatusesAsync(query, cancellationToken);
        return ResultHttpMapper.ToActionResult(this, result);
    }
}
