namespace Inventario.Contracts.Common;

/// <summary>
/// Éxito con datos y avisos opcionales (W10). Usado cuando hay <c>warnings</c> (p. ej. R13).
/// </summary>
public sealed class OperationResultDto<T>
{
    public required T Data { get; init; }

    public IReadOnlyList<WarningDto> Warnings { get; init; } = [];
}
