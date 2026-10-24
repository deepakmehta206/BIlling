using System.Reflection;
using Billing.Application.Abstractions;
using Billing.Domain.Common;
using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, IRequestContext request) : DbContext(options)
{
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<AppModule> Modules => Set<AppModule>();
    public DbSet<RestaurantModule> RestaurantModules => Set<RestaurantModule>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplyAuditColumns(modelBuilder);
        ApplySoftDeleteFilters(modelBuilder);
    }

    public override int SaveChanges()
    {
        StampAudit();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAudit();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampAudit()
    {
        var now = DateTime.UtcNow;
        var ip = InputTrim(request.IpAddress, 64);

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
            }

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                    entry.Entity.Id = Guid.NewGuid();

                if (entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = now;

                entry.Entity.CreatedBy ??= request.UserId;
                if (string.IsNullOrWhiteSpace(entry.Entity.CreatedIp))
                    entry.Entity.CreatedIp = ip;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = request.UserId;
                entry.Entity.UpdatedIp = ip;

                var deleted = entry.Property(entity => entity.IsDeleted);
                if (entry.Entity.IsDeleted && deleted.IsModified && deleted.OriginalValue == false)
                {
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = request.UserId;
                    entry.Entity.DeletedIp = ip;
                }
            }
        }
    }

    private static IReadOnlyList<Type> AuditableTypes(ModelBuilder modelBuilder) =>
        modelBuilder.Model.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .Where(type => typeof(AuditableEntity).IsAssignableFrom(type) && !type.IsAbstract)
            .Distinct()
            .ToList();

    private static void ApplyAuditColumns(ModelBuilder modelBuilder)
    {
        foreach (var type in AuditableTypes(modelBuilder))
        {
            var entity = modelBuilder.Entity(type);
            entity.Property(nameof(AuditableEntity.CreatedIp)).HasMaxLength(64);
            entity.Property(nameof(AuditableEntity.UpdatedIp)).HasMaxLength(64);
            entity.Property(nameof(AuditableEntity.DeletedIp)).HasMaxLength(64);
        }
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var type in AuditableTypes(modelBuilder))
        {
            typeof(AppDbContext)
                .GetMethod(nameof(ApplyFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type)
                .Invoke(null, [modelBuilder]);
        }
    }

    private static void ApplyFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : AuditableEntity =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(entity => !entity.IsDeleted);

    private static string? InputTrim(string? value, int max) =>
        string.IsNullOrEmpty(value) ? value : value.Length <= max ? value : value[..max];
}
