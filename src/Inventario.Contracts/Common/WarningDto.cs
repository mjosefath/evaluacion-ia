namespace Inventario.Contracts.Common;

/// <summary>Aviso W10 en respuesta exitosa (código + mensaje).</summary>
public sealed record WarningDto(string Code, string Message);
