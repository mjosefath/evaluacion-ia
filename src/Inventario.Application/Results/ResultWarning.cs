namespace Inventario.Application.Results;

/// <summary>
/// Aviso no bloqueante en éxito (W10), p. ej. producto en BAJO tras movimiento.
/// </summary>
public sealed record ResultWarning(string Code, string Message);
