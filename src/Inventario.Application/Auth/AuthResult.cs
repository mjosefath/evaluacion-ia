namespace Inventario.Application.Auth;

public sealed class AuthResult<T>
{
    private AuthResult(
        bool isSuccess,
        T? value,
        AuthErrorCode errorCode,
        string? errorMessage,
        IReadOnlyDictionary<string, string[]>? fieldErrors)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        FieldErrors = fieldErrors;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public AuthErrorCode ErrorCode { get; }

    public string? ErrorMessage { get; }

    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; }

    public static AuthResult<T> Success(T value) =>
        new(true, value, AuthErrorCode.None, null, null);

    public static AuthResult<T> Failure(
        AuthErrorCode errorCode,
        string? errorMessage = null,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null) =>
        new(false, default, errorCode, errorMessage, fieldErrors);
}
