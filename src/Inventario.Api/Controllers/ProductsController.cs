using Inventario.Api.Http;
using Inventario.Application.Products;
using Inventario.Contracts.Common;
using Inventario.Contracts.Products;
using Inventario.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Controllers;

[ApiController]
[Authorize(Policy = PermissionCodes.ProductosVer)]
[Route("api/products")]
public sealed class ProductsController(IProductQueryService products) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] ProductListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await products.ListAsync(query, cancellationToken);
        return ResultHttpMapper.ToActionResult(this, result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await products.GetByIdAsync(id, cancellationToken);
        return ResultHttpMapper.ToActionResult(this, result);
    }
}
