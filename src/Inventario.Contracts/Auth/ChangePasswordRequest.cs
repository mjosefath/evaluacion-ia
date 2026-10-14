namespace Inventario.Contracts.Auth;

/// <summary>
/// POST /api/auth/change-password (Bearer). No loguear contraseñas (R3).
/// La confirmación de coincidencia puede validarse en la Web antes de llamar.
/// </summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
