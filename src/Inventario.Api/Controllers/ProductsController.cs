using Inventario.Api.Http;
using Inventario.Application.Products;
using Inventario.Contracts.Common;
using Inventario.Contracts.Products;
using Inventario.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController(
    IProductQueryService products,
    IProductCommandService productCommands) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.ProductosVer)]
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
    [Authorize(Policy = PermissionCodes.ProductosVer)]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await products.GetByIdAsync(id, cancellationToken);
        return ResultHttpMapper.ToActionResult(this, result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.ProductosCrear)]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productCommands.CreateAsync(request, cancellationToken);
        return ResultHttpMapper.ToActionResult(this, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PermissionCodes.ProductosEditar)]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productCommands.UpdateAsync(id, request, cancellationToken);
        return ResultHttpMapper.ToActionResult(this, result);
    }

    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = PermissionCodes.ProductosActivar)]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(
        int id,
        [FromBody] ProductStatusChangeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productCommands.DeactivateAsync(id, request, cancellationToken);
        return ResultHttpMapper.ToActionResult(this, result);
    }

    [HttpPost("{id:int}/activate")]
    [Authorize(Policy = PermissionCodes.ProductosActivar)]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Activate(
        int id,
        [FromBody] ProductStatusChangeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productCommands.ActivateAsync(id, request, cancellationToken);
        return ResultHttpMapper.ToActionResult(this, result);
    }

    [HttpGet("lookup")]
    [Authorize(Policy = PermissionCodes.MovimientosAny)]
    [ProducesResponseType(typeof(PagedResult<ProductLookupItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Lookup(
        [FromQuery] ProductLookupQuery query,
        CancellationToken cancellationToken)
    {
        var result = await products.LookupAsync(query, cancellationToken);
        return ResultHttpMapper.ToActionResult(this, result);
    }
}
