using Inventario.ApiClient.Auth;
using Inventario.ApiClient.Movements;
using Inventario.Contracts.Movements;
using Inventario.Contracts.Security;
using Inventario.Web.Auth;
using Inventario.Web.Errors;
using Inventario.Web.Models;
using Inventario.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

[Authorize(Policy = PermissionCodes.MovimientosAny)]
[Route("movements")]
public sealed class MovementsController(
    IInventarioMovementsApi movementsApi,
    AuthSessionService authSession) : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Movimientos";
        var model = PrepareForm(new CreateMovementFormModel());
        return View(model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateMovementFormModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return FormErrorResult(model, 400);
        }

        try
        {
            var result = await movementsApi.CreateAsync(model.ToRequest(), cancellationToken);
            var data = result.Data;
            var warning = result.Warnings.FirstOrDefault();

            if (warning is not null)
            {
                return Json(new
                {
                    keepType = true,
                    notify = new { type = "warning", message = warning.Message }
                });
            }

            var message =
                $"Guardado. {data.ProductName}: existencia {data.StockBefore} → {data.StockAfter}.";
            return Json(new
            {
                keepType = true,
                notify = new { type = "success", message }
            });
        }
        catch (InventarioApiException ex)
        {
            return await HandleCreateApiErrorAsync(model, ex, cancellationToken);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, WebUiMessages.ConnectionFailed);
            return FormErrorResult(model, 400);
        }
        catch (TaskCanceledException)
        {
            ModelState.AddModelError(string.Empty, WebUiMessages.Timeout);
            return FormErrorResult(model, 400);
        }
    }

    private async Task<IActionResult> HandleCreateApiErrorAsync(
        CreateMovementFormModel model,
        InventarioApiException ex,
        CancellationToken cancellationToken)
    {
        var translation = ApiErrorTranslator.Translate(ex.Error, ApiErrorSurface.Form);

        switch (translation.Disposition)
        {
            case ApiErrorDisposition.ApplyModelState:
                ApiErrorTranslator.ApplyToModelState(ModelState, translation, MapField);
                return FormErrorResult(model, 400);

            case ApiErrorDisposition.FormMessage:
            case ApiErrorDisposition.TempDataError:
                ModelState.AddModelError(
                    string.Empty,
                    translation.Message ?? ex.Error.Detail ?? WebUiMessages.Unexpected);
                return FormErrorResult(
                    model,
                    ex.StatusCode is >= 400 and < 600 ? ex.StatusCode : 400);

            case ApiErrorDisposition.RedirectLoginSessionExpired:
                await authSession.SignOutAsync(cancellationToken);
                return Json(new
                {
                    redirectUrl = Url.Action("Login", "Account", new { sessionExpired = true })
                });

            case ApiErrorDisposition.RedirectAccessDenied:
                return Json(new { redirectUrl = Url.Action("AccessDenied", "Account") });

            default:
                ModelState.AddModelError(string.Empty, WebUiMessages.Unexpected);
                return FormErrorResult(model, 400);
        }
    }

    private IActionResult FormErrorResult(CreateMovementFormModel model, int statusCode)
    {
        Response.StatusCode = statusCode;
        PrepareForm(model);
        return PartialView("_MovementCreateForm", model);
    }

    private CreateMovementFormModel PrepareForm(CreateMovementFormModel form)
    {
        var types = BuildPermittedTypes();
        ViewBag.PermittedTypes = types;

        if (string.IsNullOrWhiteSpace(form.Type) && types.Count > 0)
        {
            form.Type = types[0].Code;
        }

        return form;
    }

    private List<MovementTypeOption> BuildPermittedTypes()
    {
        var types = new List<MovementTypeOption>();
        if (PermissionClaims.Has(User, PermissionCodes.MovimientosEntrada))
        {
            types.Add(new MovementTypeOption("ENTRADA", "Entrada"));
        }

        if (PermissionClaims.Has(User, PermissionCodes.MovimientosSalida))
        {
            types.Add(new MovementTypeOption("SALIDA", "Salida"));
        }

        if (PermissionClaims.Has(User, PermissionCodes.MovimientosAjuste))
        {
            types.Add(new MovementTypeOption("AJUSTE", "Ajuste"));
        }

        return types;
    }

    private static string MapField(string apiKey) =>
        apiKey.ToLowerInvariant() switch
        {
            "type" => nameof(CreateMovementFormModel.Type),
            "productid" => nameof(CreateMovementFormModel.ProductId),
            "quantity" => nameof(CreateMovementFormModel.Quantity),
            "reason" => nameof(CreateMovementFormModel.Reason),
            _ => apiKey
        };
}
