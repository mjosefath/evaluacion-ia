using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Inventario.Web.Feedback;

/// <summary>
/// Llaves tipadas TempData para avisos (análisis 03, W3–W4). No usar literales sueltos como llave.
/// </summary>
public static class FlashMessages
{
    public const string Success = "Success";
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Error = "Error";

    public static void SetSuccess(ITempDataDictionary tempData, string message) =>
        tempData[Success] = message;

    public static void SetInfo(ITempDataDictionary tempData, string message) =>
        tempData[Info] = message;

    public static void SetWarning(ITempDataDictionary tempData, string message) =>
        tempData[Warning] = message;

    public static void SetError(ITempDataDictionary tempData, string message) =>
        tempData[Error] = message;
}
