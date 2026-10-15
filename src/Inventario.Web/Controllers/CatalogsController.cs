using Inventario.ApiClient.Auth;
using Inventario.ApiClient.Catalogs;
using Inventario.Contracts.Catalogs;
using Inventario.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

/// <summary>
/// Proxy JSON de catálogos para Tom Select (PG9). La Web usa cookie; ApiClient adjunta Bearer.
/// </summary>
[Authorize]
[Route("catalogs")]
[ApiController]
public sealed class CatalogsController(IInventarioCatalogsApi catalogs) : ControllerBase
{
    /// <param name="activeOnly">true = solo activos (captura PG9); false/omitido = todos (filtros FL6).</param>
    [HttpGet("categories")]
    public async Task<ActionResult<PagedResult<CategoryListItemDto>>> Categories(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await catalogs.ListCategoriesAsync(
                new CategoryListQuery
                {
                    Search = search,
                    Page = page,
                    PageSize = pageSize,
                    StatusId = activeOnly ? (byte)1 : null
                },
                cancellationToken);
            return Ok(result);
        }
        catch (InventarioApiException ex)
        {
            return StatusCode(ex.Error.Status, ex.Error);
        }
    }

    [HttpGet("movement-types")]
    public async Task<ActionResult<PagedResult<MovementTypeListItemDto>>> MovementTypes(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await catalogs.ListMovementTypesAsync(
                new MovementTypeListQuery
                {
                    Search = search,
                    Page = page,
                    PageSize = pageSize,
                    StatusId = activeOnly ? (byte)1 : null
                },
                cancellationToken);
            return Ok(result);
        }
        catch (InventarioApiException ex)
        {
            return StatusCode(ex.Error.Status, ex.Error);
        }
    }

    [HttpGet("record-statuses")]
    public async Task<ActionResult<PagedResult<RecordStatusListItemDto>>> RecordStatuses(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await catalogs.ListRecordStatusesAsync(
                new RecordStatusListQuery
                {
                    Search = search,
                    Page = page,
                    PageSize = pageSize
                },
                cancellationToken);
            return Ok(result);
        }
        catch (InventarioApiException ex)
        {
            return StatusCode(ex.Error.Status, ex.Error);
        }
    }
}
