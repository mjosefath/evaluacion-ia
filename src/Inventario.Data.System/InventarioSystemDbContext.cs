using Microsoft.EntityFrameworkCore;

namespace Inventario.Data.System;

/// <summary>
/// DbContext Sistema alineado a database/system/001–003. Sin migraciones al arrancar.
/// </summary>
public sealed class InventarioSystemDbContext : DbContext
{
    public InventarioSystemDbContext(DbContextOptions<InventarioSystemDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.IdentityUserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(e => e.DisplayName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(7)");

            entity.Property(e => e.CreatedByIdentityUserId)
                .HasMaxLength(450);

            entity.Property(e => e.ModifiedAtUtc)
                .HasColumnType("datetime2(7)");

            entity.Property(e => e.ModifiedByIdentityUserId)
                .HasMaxLength(450);

            entity.HasIndex(e => e.IdentityUserId)
                .IsUnique()
                .HasDatabaseName("UX_UserProfiles_IdentityUserId");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).UseIdentityColumn();

            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Module)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(e => e.Code)
                .IsUnique()
                .HasDatabaseName("UX_Permissions_Code");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");

            entity.HasKey(e => new { e.RoleName, e.PermissionId });

            entity.Property(e => e.RoleName)
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(e => e.PermissionId)
                .HasDatabaseName("IX_RolePermissions_PermissionId");

            entity.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_RolePermissions_Permissions_PermissionId");
        });

        modelBuilder.Entity<AccessLog>(entity =>
        {
            entity.ToTable("AccessLogs");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).UseIdentityColumn();

            entity.Property(e => e.OccurredAtUtc)
                .HasColumnType("datetime2(7)");

            entity.Property(e => e.IdentityUserId)
                .HasMaxLength(450);

            entity.Property(e => e.UserNameAttempted)
                .HasMaxLength(256);

            entity.Property(e => e.EventType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .HasMaxLength(64);

            entity.Property(e => e.UserAgent)
                .HasMaxLength(512);

            entity.Property(e => e.Detail)
                .HasMaxLength(500);

            entity.HasIndex(e => e.OccurredAtUtc)
                .HasDatabaseName("IX_AccessLogs_OccurredAtUtc");

            entity.HasIndex(e => e.IdentityUserId)
                .HasDatabaseName("IX_AccessLogs_IdentityUserId")
                .HasFilter("[IdentityUserId] IS NOT NULL");

            entity.HasIndex(e => e.EventType)
                .HasDatabaseName("IX_AccessLogs_EventType");
        });
    }
}
