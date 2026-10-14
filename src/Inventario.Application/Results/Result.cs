namespace Inventario.Application.Results;

/// <summary>
/// Resultado de caso de uso sin valor (análisis 03, E1).
/// </summary>
public class Result
{
    protected Result(
        bool isSuccess,
        string? code,
        string? message,
        string? rule,
        IReadOnlyDictionary<string, string[]>? fieldErrors,
        IReadOnlyList<ResultWarning>? warnings)
    {
        IsSuccess = isSuccess;
        Code = code;
        Message = message;
        Rule = rule;
        FieldErrors = fieldErrors;
        Warnings = warnings;
    }

    public bool IsSuccess { get; }

    /// <summary>Código E2 en fallo; null en éxito.</summary>
    public string? Code { get; }

    /// <summary>Mensaje para el usuario (catálogo Application).</summary>
    public string? Message { get; }

    /// <summary>Subcódigo de business.rule.</summary>
    public string? Rule { get; }

    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; }

    /// <summary>Avisos W10 en éxito (opcionales).</summary>
    public IReadOnlyList<ResultWarning>? Warnings { get; }

    public static Result Success(IReadOnlyList<ResultWarning>? warnings = null) =>
        new(true, null, null, null, null, warnings);

    public static Result Failure(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null,
        string? rule = null) =>
        new(false, code, message, rule, fieldErrors, null);

    public static Result Validation(
        IReadOnlyDictionary<string, string[]> fieldErrors,
        string? message = null) =>
        Failure(
            ErrorCodes.Validation,
            message ?? MessageCatalog.ValidationFailed,
            fieldErrors);

    public static Result BusinessRule(string rule, string message) =>
        Failure(ErrorCodes.BusinessRule, message, rule: rule);
}

/// <summary>
/// Resultado de caso de uso con valor (análisis 03, E1).
/// </summary>
public sealed class Result<T> : Result
{
    private Result(
        bool isSuccess,
        T? value,
        string? code,
        string? message,
        string? rule,
        IReadOnlyDictionary<string, string[]>? fieldErrors,
        IReadOnlyList<ResultWarning>? warnings)
        : base(isSuccess, code, message, rule, fieldErrors, warnings)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value, IReadOnlyList<ResultWarning>? warnings = null) =>
        new(true, value, null, null, null, null, warnings);

    public new static Result<T> Failure(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null,
        string? rule = null) =>
        new(false, default, code, message, rule, fieldErrors, null);

    public new static Result<T> Validation(
        IReadOnlyDictionary<string, string[]> fieldErrors,
        string? message = null) =>
        Failure(
            ErrorCodes.Validation,
            message ?? MessageCatalog.ValidationFailed,
            fieldErrors);

    public new static Result<T> BusinessRule(string rule, string message) =>
        Failure(ErrorCodes.BusinessRule, message, rule: rule);
}
