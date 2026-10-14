namespace Inventario.Application.Services;

public interface IPermissionLookup
{
    Task<IReadOnlyList<string>> GetPermissionCodesForRolesAsync(
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken = default);
}
