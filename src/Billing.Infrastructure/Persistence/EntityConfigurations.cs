using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Infrastructure.Persistence;

public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.ToTable("Restaurants");
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(24).IsRequired();
        builder.Property(x => x.LegalName).HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(400);
        builder.Property(x => x.City).HasMaxLength(80);
        builder.Property(x => x.State).HasMaxLength(80);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Gstin).HasMaxLength(15);
        builder.Property(x => x.OwnerName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.OwnerPhone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.UpiId).HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users");
        builder.Property(x => x.FullName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.LastLoginIp).HasMaxLength(64);
        builder.HasIndex(x => x.Email).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.Restaurant)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.Property(x => x.Name).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(240).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Role)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AppModuleConfiguration : IEntityTypeConfiguration<AppModule>
{
    public void Configure(EntityTypeBuilder<AppModule> builder)
    {
        builder.ToTable("Modules");
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(240).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

public class RestaurantModuleConfiguration : IEntityTypeConfiguration<RestaurantModule>
{
    public void Configure(EntityTypeBuilder<RestaurantModule> builder)
    {
        builder.ToTable("RestaurantModules");
        builder.HasIndex(x => new { x.RestaurantId, x.ModuleId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.Restaurant)
            .WithMany(x => x.Modules)
            .HasForeignKey(x => x.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Module)
            .WithMany(x => x.Restaurants)
            .HasForeignKey(x => x.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.Property(x => x.ActorEmail).HasMaxLength(256);
        builder.Property(x => x.RestaurantName).HasMaxLength(160);
        builder.Property(x => x.Action).HasMaxLength(80).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(64);
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.RestaurantId, x.CreatedAt });
    }
}
