using Inventario.ApiClient.Products;
using Inventario.Contracts.Products;
using Inventario.Contracts.Security;
using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

[Authorize(Policy = PermissionCodes.ProductosVer)]
[Route("products")]
public sealed class ProductsController(IInventarioProductsApi productsApi) : Controller
{
    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(
        string? search,
        int? categoryId,
        string? statusId,
        int page = 1,
        int pageSize = 20,
        string? sort = null,
        string? dir = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Productos";

        var (resolvedStatusId, statusAll, statusLabel) = ResolveStatusFilter(statusId);

        var query = new ProductListQuery
        {
            Search = search,
            CategoryId = categoryId,
            StatusId = resolvedStatusId,
            Page = page < 1 ? 1 : page,
            PageSize = pageSize is 20 or 50 or 100 ? pageSize : 20,
            Sort = string.IsNullOrWhiteSpace(sort) ? "code" : sort,
            Dir = string.IsNullOrWhiteSpace(dir) ? "asc" : dir
        };

        var result = await productsApi.ListAsync(query, cancellationToken);

        string? categoryLabel = null;
        if (categoryId.HasValue)
        {
            categoryLabel = result.Items.FirstOrDefault(i => i.CategoryId == categoryId.Value)?.CategoryName
                            ?? categoryId.Value.ToString();
        }

        if (!statusAll && resolvedStatusId.HasValue && string.IsNullOrEmpty(statusLabel))
        {
            statusLabel = result.Items.FirstOrDefault(i => i.StatusId == resolvedStatusId.Value)?.StatusName
                          ?? (resolvedStatusId == 1 ? "Activo" : "Inactivo");
        }

        return View(new ProductIndexViewModel
        {
            Query = query,
            Result = result,
            CategoryLabel = categoryLabel,
            StatusLabel = statusLabel,
            StatusFilterAll = statusAll
        });
    }

    /// <summary>
    /// Sin clave → Activo (1). statusId= (vacío) → todos. Valor numérico → filtro.
    /// </summary>
    private (byte? StatusId, bool All, string? Label) ResolveStatusFilter(string? statusId)
    {
        if (!Request.Query.ContainsKey("statusId"))
        {
            return (1, false, "Activo");
        }

        if (string.IsNullOrWhiteSpace(statusId))
        {
            return (null, true, null);
        }

        if (byte.TryParse(statusId, out var parsed))
        {
            var label = parsed == 1 ? "Activo" : parsed == 2 ? "Inactivo" : null;
            return (parsed, false, label);
        }

        return (1, false, "Activo");
    }
}
