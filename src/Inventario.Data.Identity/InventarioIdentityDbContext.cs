using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Data.Identity;

/// <summary>
/// DbContext Identity alineado a database/identity/001. Sin migraciones al arrancar.
/// </summary>
public sealed class InventarioIdentityDbContext : IdentityDbContext<IdentityUser>
{
    public InventarioIdentityDbContext(DbContextOptions<InventarioIdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.HasKey(e => e.Id);

            // El script no define DEFAULT; la aplicación asigna el Id.
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.UserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(e => e.TokenHash)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.ReplacedByTokenHash)
                .HasMaxLength(128);

            entity.Property(e => e.ExpiresAtUtc)
                .HasColumnType("datetime2(7)");

            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(7)");

            entity.Property(e => e.RevokedAtUtc)
                .HasColumnType("datetime2(7)");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_RefreshTokens_UserId");

            entity.HasIndex(e => e.TokenHash)
                .HasDatabaseName("IX_RefreshTokens_TokenHash");

            entity.HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_RefreshTokens_AspNetUsers_UserId");
        });
    }
}
