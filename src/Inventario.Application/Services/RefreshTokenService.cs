using System.Security.Cryptography;
using System.Text;
using Inventario.Application.Options;
using Inventario.Data.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Inventario.Application.Services;

public sealed class RefreshTokenService(
    InventarioIdentityDbContext identityDb,
    IOptions<JwtOptions> jwtOptions) : IRefreshTokenService
{
    public string HashToken(string plainToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public async Task<(string PlainToken, RefreshToken Entity)> IssueAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var (plain, entity) = CreateToken(userId);
        identityDb.RefreshTokens.Add(entity);
        await identityDb.SaveChangesAsync(cancellationToken);
        return (plain, entity);
    }

    public async Task<RefreshToken?> FindActiveByPlainTokenAsync(
        string plainToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plainToken))
        {
            return null;
        }

        var hash = HashToken(plainToken);
        var now = DateTime.UtcNow;

        return await identityDb.RefreshTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == hash
                     && t.RevokedAtUtc == null
                     && t.ExpiresAtUtc > now,
                cancellationToken);
    }

    public async Task<(string PlainToken, RefreshToken Entity)?> RotateAsync(
        RefreshToken current,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await identityDb.Database.BeginTransactionAsync(cancellationToken);

        var tracked = await identityDb.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == current.Id, cancellationToken);

        if (tracked is null || tracked.RevokedAtUtc is not null || tracked.ExpiresAtUtc <= DateTime.UtcNow)
        {
            await tx.RollbackAsync(cancellationToken);
            return null;
        }

        var (plain, next) = CreateToken(tracked.UserId);
        identityDb.RefreshTokens.Add(next);

        tracked.RevokedAtUtc = DateTime.UtcNow;
        tracked.ReplacedByTokenHash = next.TokenHash;

        await identityDb.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return (plain, next);
    }

    public async Task<bool> RevokeAsync(
        string userId,
        string plainToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plainToken))
        {
            return false;
        }

        var hash = HashToken(plainToken);
        var token = await identityDb.RefreshTokens
            .FirstOrDefaultAsync(
                t => t.UserId == userId && t.TokenHash == hash && t.RevokedAtUtc == null,
                cancellationToken);

        if (token is null)
        {
            return false;
        }

        token.RevokedAtUtc = DateTime.UtcNow;
        await identityDb.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var active = await identityDb.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.RevokedAtUtc = now;
        }

        if (active.Count > 0)
        {
            await identityDb.SaveChangesAsync(cancellationToken);
        }
    }

    private (string PlainToken, RefreshToken Entity) CreateToken(string userId)
    {
        var plain = CreatePlainToken();
        var now = DateTime.UtcNow;
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(plain),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(jwtOptions.Value.RefreshTokenDays)
        };

        return (plain, entity);
    }

    private static string CreatePlainToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
