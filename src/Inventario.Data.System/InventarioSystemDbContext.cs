using Microsoft.EntityFrameworkCore;

namespace Inventario.Data.System;

/// <summary>
/// DbContext Sistema alineado a database/system/001–007 (estándar E1–E9). Sin migraciones al arrancar.
/// </summary>
public sealed class InventarioSystemDbContext : DbContext
{
    public InventarioSystemDbContext(DbContextOptions<InventarioSystemDbContext> options)
        : base(options)
    {
    }

    public DbSet<RecordStatus> RecordStatuses => Set<RecordStatus>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<MovementType> MovementTypes => Set<MovementType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecordStatus>(entity =>
        {
            entity.ToTable("RecordStatuses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(10).IsRequired().IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UX_RecordStatuses_Code");
            entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("UX_RecordStatuses_Name");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.IdentityUserId).HasMaxLength(450).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.StatusId).IsRequired().HasDefaultValue(RecordStatusIds.Active);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.IdentityUserId)
                .IsUnique()
                .HasDatabaseName("UX_UserProfiles_IdentityUserId");

            entity.HasOne<RecordStatus>()
                .WithMany()
                .HasForeignKey(e => e.StatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_UserProfiles_RecordStatuses_StatusId");

            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_UserProfiles_UserProfiles_CreatedBy");

            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_UserProfiles_UserProfiles_UpdatedBy");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Module).HasMaxLength(100).IsRequired();
            entity.Property(e => e.StatusId).IsRequired().HasDefaultValue(RecordStatusIds.Active);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UX_Permissions_Code");

            entity.HasOne<RecordStatus>()
                .WithMany()
                .HasForeignKey(e => e.StatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Permissions_RecordStatuses_StatusId");

            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Permissions_UserProfiles_CreatedBy");

            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Permissions_UserProfiles_UpdatedBy");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.RoleName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.StatusId).IsRequired().HasDefaultValue(RecordStatusIds.Active);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => new { e.RoleName, e.PermissionId })
                .IsUnique()
                .HasDatabaseName("UX_RolePermissions_RoleName_PermissionId");

            entity.HasIndex(e => e.PermissionId)
                .HasDatabaseName("IX_RolePermissions_PermissionId");

            entity.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_RolePermissions_Permissions_PermissionId");

            entity.HasOne<RecordStatus>()
                .WithMany()
                .HasForeignKey(e => e.StatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_RolePermissions_RecordStatuses_StatusId");

            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_RolePermissions_UserProfiles_CreatedBy");

            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_RolePermissions_UserProfiles_UpdatedBy");
        });

        modelBuilder.Entity<AccessLog>(entity =>
        {
            entity.ToTable("AccessLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.UserNameAttempted).HasMaxLength(256);
            entity.Property(e => e.EventType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.UserAgent).HasMaxLength(512);
            entity.Property(e => e.Detail).HasMaxLength(500);

            entity.HasIndex(e => e.CreatedAtUtc).HasDatabaseName("IX_AccessLogs_CreatedAtUtc");
            entity.HasIndex(e => e.CreatedBy)
                .HasDatabaseName("IX_AccessLogs_CreatedBy")
                .HasFilter("[CreatedBy] IS NOT NULL");
            entity.HasIndex(e => e.EventType).HasDatabaseName("IX_AccessLogs_EventType");

            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AccessLogs_UserProfiles_CreatedBy");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.StatusId).IsRequired().HasDefaultValue(RecordStatusIds.Active);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("UX_Categories_Name");

            entity.HasOne<RecordStatus>()
                .WithMany()
                .HasForeignKey(e => e.StatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Categories_RecordStatuses_StatusId");

            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Categories_UserProfiles_CreatedBy");

            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Categories_UserProfiles_UpdatedBy");
        });

        modelBuilder.Entity<MovementType>(entity =>
        {
            entity.ToTable("MovementTypes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.Code).HasMaxLength(10).IsRequired().IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(30).IsRequired();
            entity.Property(e => e.StatusId).IsRequired().HasDefaultValue(RecordStatusIds.Active);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnType("datetime2(3)");
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UX_MovementTypes_Code");

            entity.HasOne<RecordStatus>()
                .WithMany()
                .HasForeignKey(e => e.StatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MovementTypes_RecordStatuses_StatusId");

            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MovementTypes_UserProfiles_CreatedBy");

            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MovementTypes_UserProfiles_UpdatedBy");
        });
    }
}
