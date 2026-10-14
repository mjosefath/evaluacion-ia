using Inventario.Application.Results;

namespace Inventario.Application.Auth;

/// <summary>
/// Puente sobre <see cref="Result{T}"/> (análisis 03 / A1). Preferir Result&lt;T&gt; en código nuevo.
/// Migración completa de AuthService/mappers → T03-06/07.
/// </summary>
[Obsolete("Usar Inventario.Application.Results.Result<T> con ErrorCodes (análisis 03).")]
public sealed class AuthResult<T>
{
    private AuthResult(Result<T> inner)
    {
        Inner = inner;
    }

    /// <summary>Resultado general subyacente.</summary>
    public Result<T> Inner { get; }

    public bool IsSuccess => Inner.IsSuccess;

    public T? Value => Inner.Value;

    public AuthErrorCode ErrorCode => AuthErrorCodeMapper.FromErrorCode(Inner.Code);

    public string? ErrorMessage => Inner.Message;

    public IReadOnlyDictionary<string, string[]>? FieldErrors => Inner.FieldErrors;

    public static AuthResult<T> Success(T value) =>
        new(Result<T>.Success(value));

    public static AuthResult<T> Failure(
        AuthErrorCode errorCode,
        string? errorMessage = null,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null)
    {
        var code = AuthErrorCodeMapper.ToErrorCode(errorCode);
        var message = string.IsNullOrWhiteSpace(errorMessage)
            ? AuthErrorCodeMapper.DefaultMessage(errorCode)
            : errorMessage;

        return new AuthResult<T>(Result<T>.Failure(code, message, fieldErrors));
    }

    public static AuthResult<T> FromResult(Result<T> result) => new(result);
}
