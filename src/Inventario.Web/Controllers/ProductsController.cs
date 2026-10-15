using Inventario.ApiClient.Auth;
using Inventario.ApiClient.Products;
using Inventario.Contracts.Products;
using Inventario.Contracts.Security;
using Inventario.Web.Auth;
using Inventario.Web.Errors;
using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

[Authorize(Policy = PermissionCodes.ProductosVer)]
[Route("products")]
public sealed class ProductsController(
    IInventarioProductsApi productsApi,
    AuthSessionService authSession) : Controller
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

    [HttpGet("create")]
    [Authorize(Policy = PermissionCodes.ProductosCrear)]
    public IActionResult CreateForm() =>
        PartialView("_ProductCreateForm", new ProductCreateFormModel());

    [HttpPost("create")]
    [Authorize(Policy = PermissionCodes.ProductosCrear)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ProductCreateFormModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ProductFormResult("_ProductCreateForm", model, 400);
        }

        try
        {
            await productsApi.CreateAsync(model.ToRequest(), cancellationToken);
            return Json(new
            {
                refresh = true,
                notify = new { type = "success", message = "Producto creado." }
            });
        }
        catch (InventarioApiException ex)
        {
            return await HandleProductFormApiErrorAsync(
                "_ProductCreateForm",
                model,
                ex,
                MapCreateField,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, WebUiMessages.ConnectionFailed);
            return ProductFormResult("_ProductCreateForm", model, 400);
        }
        catch (TaskCanceledException)
        {
            ModelState.AddModelError(string.Empty, WebUiMessages.Timeout);
            return ProductFormResult("_ProductCreateForm", model, 400);
        }
    }

    [HttpGet("edit/{id:int}")]
    [Authorize(Policy = PermissionCodes.ProductosEditar)]
    public async Task<IActionResult> EditForm(int id, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await productsApi.GetByIdAsync(id, cancellationToken);
            return PartialView("_ProductEditForm", ProductEditFormModel.FromDetail(detail));
        }
        catch (InventarioApiException ex)
        {
            // El filtro global maneja 401/403/404 de página; aquí devolvemos HTML simple para modal.
            Response.StatusCode = ex.StatusCode is >= 400 and < 600 ? ex.StatusCode : 404;
            return Content(
                $"<p class=\"form-error\">{ex.Error.Detail ?? WebUiMessages.Unexpected}</p>",
                "text/html");
        }
    }

    [HttpPost("edit/{id:int}")]
    [Authorize(Policy = PermissionCodes.ProductosEditar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        ProductEditFormModel model,
        CancellationToken cancellationToken)
    {
        model.Id = id;

        if (!ModelState.IsValid)
        {
            return ProductFormResult("_ProductEditForm", model, 400);
        }

        try
        {
            await productsApi.UpdateAsync(id, model.ToRequest(), cancellationToken);
            return Json(new
            {
                refresh = true,
                notify = new { type = "success", message = "Producto actualizado." }
            });
        }
        catch (InventarioApiException ex)
        {
            return await HandleProductFormApiErrorAsync(
                "_ProductEditForm",
                model,
                ex,
                MapEditField,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, WebUiMessages.ConnectionFailed);
            return ProductFormResult("_ProductEditForm", model, 400);
        }
        catch (TaskCanceledException)
        {
            ModelState.AddModelError(string.Empty, WebUiMessages.Timeout);
            return ProductFormResult("_ProductEditForm", model, 400);
        }
    }

    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = PermissionCodes.ProductosActivar)]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Deactivate(
        int id,
        ProductStatusChangeRequest request,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(
            id,
            request,
            deactivate: true,
            successMessage: "Producto desactivado.",
            cancellationToken);

    [HttpPost("{id:int}/activate")]
    [Authorize(Policy = PermissionCodes.ProductosActivar)]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Activate(
        int id,
        ProductStatusChangeRequest request,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(
            id,
            request,
            deactivate: false,
            successMessage: "Producto activado.",
            cancellationToken);

    private async Task<IActionResult> ChangeStatusAsync(
        int id,
        ProductStatusChangeRequest request,
        bool deactivate,
        string successMessage,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return JsonNotifyError(ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                ?? WebUiMessages.Unexpected);
        }

        try
        {
            if (deactivate)
            {
                await productsApi.DeactivateAsync(id, request, cancellationToken);
            }
            else
            {
                await productsApi.ActivateAsync(id, request, cancellationToken);
            }

            return Json(new
            {
                refresh = true,
                notify = new { type = "success", message = successMessage }
            });
        }
        catch (InventarioApiException ex)
        {
            return await HandleStatusAjaxApiErrorAsync(ex, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return JsonNotifyError(WebUiMessages.ConnectionFailed);
        }
        catch (TaskCanceledException)
        {
            return JsonNotifyError(WebUiMessages.Timeout);
        }
    }

    private async Task<IActionResult> HandleStatusAjaxApiErrorAsync(
        InventarioApiException ex,
        CancellationToken cancellationToken)
    {
        var translation = ApiErrorTranslator.Translate(ex.Error, ApiErrorSurface.Form);

        switch (translation.Disposition)
        {
            case ApiErrorDisposition.RedirectLoginSessionExpired:
                await authSession.SignOutAsync(cancellationToken);
                return Json(new
                {
                    redirectUrl = Url.Action("Login", "Account", new { sessionExpired = true })
                });

            case ApiErrorDisposition.RedirectAccessDenied:
                return Json(new { redirectUrl = Url.Action("AccessDenied", "Account") });

            default:
                var message = translation.Message
                              ?? ex.Error.Detail
                              ?? WebUiMessages.Unexpected;
                if (translation.FieldErrors is { Count: > 0 })
                {
                    message = translation.FieldErrors.Values
                                  .SelectMany(v => v)
                                  .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                              ?? message;
                }

                return JsonNotifyError(message);
        }
    }

    private static IActionResult JsonNotifyError(string message) =>
        new JsonResult(new { notify = new { type = "error", message } })
        {
            StatusCode = StatusCodes.Status400BadRequest
        };

    private async Task<IActionResult> HandleProductFormApiErrorAsync(
        string partialName,
        object model,
        InventarioApiException ex,
        Func<string, string> mapField,
        CancellationToken cancellationToken)
    {
        var translation = ApiErrorTranslator.Translate(ex.Error, ApiErrorSurface.Form);

        switch (translation.Disposition)
        {
            case ApiErrorDisposition.ApplyModelState:
                ApiErrorTranslator.ApplyToModelState(ModelState, translation, mapField);
                return ProductFormResult(partialName, model, 400);

            case ApiErrorDisposition.FormMessage:
            case ApiErrorDisposition.TempDataError:
                ModelState.AddModelError(
                    string.Empty,
                    translation.Message ?? ex.Error.Detail ?? WebUiMessages.Unexpected);
                return ProductFormResult(
                    partialName,
                    model,
                    ex.StatusCode is >= 400 and < 600 ? ex.StatusCode : 400);

            case ApiErrorDisposition.RedirectLoginSessionExpired:
                await authSession.SignOutAsync(cancellationToken);
                if (IsModalAjaxRequest())
                {
                    return Json(new
                    {
                        redirectUrl = Url.Action("Login", "Account", new { sessionExpired = true })
                    });
                }

                return RedirectToAction("Login", "Account", new { sessionExpired = true });

            case ApiErrorDisposition.RedirectAccessDenied:
                if (IsModalAjaxRequest())
                {
                    return Json(new { redirectUrl = Url.Action("AccessDenied", "Account") });
                }

                return RedirectToAction("AccessDenied", "Account");

            default:
                ModelState.AddModelError(string.Empty, WebUiMessages.Unexpected);
                return ProductFormResult(partialName, model, 400);
        }
    }

    private IActionResult ProductFormResult(string partialName, object model, int statusCode)
    {
        Response.StatusCode = statusCode;
        return PartialView(partialName, model);
    }

    private bool IsModalAjaxRequest() =>
        string.Equals(
            Request.Headers.XRequestedWith.ToString(),
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);

    private static string MapCreateField(string apiKey) =>
        apiKey.ToLowerInvariant() switch
        {
            "code" => nameof(ProductCreateFormModel.Code),
            "name" => nameof(ProductCreateFormModel.Name),
            "categoryid" => nameof(ProductCreateFormModel.CategoryId),
            "unitprice" => nameof(ProductCreateFormModel.UnitPrice),
            "minstock" => nameof(ProductCreateFormModel.MinStock),
            _ => apiKey
        };

    private static string MapEditField(string apiKey) =>
        apiKey.ToLowerInvariant() switch
        {
            "name" => nameof(ProductEditFormModel.Name),
            "categoryid" => nameof(ProductEditFormModel.CategoryId),
            "unitprice" => nameof(ProductEditFormModel.UnitPrice),
            "minstock" => nameof(ProductEditFormModel.MinStock),
            "rowversion" => nameof(ProductEditFormModel.RowVersion),
            _ => apiKey
        };

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
