using Billing.Application.Common;
using Billing.Domain.Entities;
using Billing.Domain.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Billing.Infrastructure.Persistence;

public class DatabaseInitializer(IServiceProvider services, ILogger<DatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        await SeedAsync(scope.ServiceProvider, cancellationToken);
        logger.LogInformation("Billing database is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var passwords = services.GetRequiredService<IPasswordHasher<AppUser>>();
        var now = DateTime.UtcNow;

        foreach (var (roleName, description) in RoleNames.All)
        {
            var role = await db.Roles.FirstOrDefaultAsync(item => item.Name == roleName, cancellationToken);
            if (role is null)
            {
                db.Roles.Add(new Role
                {
                    Name = roleName,
                    Description = description,
                    CreatedAt = now,
                    CreatedIp = "seed"
                });
            }
            else if (role.Description != description)
            {
                role.Description = description;
            }
        }

        foreach (var definition in ModuleCodes.All)
        {
            var module = await db.Modules.FirstOrDefaultAsync(item => item.Code == definition.Code, cancellationToken);
            if (module is null)
            {
                db.Modules.Add(new AppModule
                {
                    Code = definition.Code,
                    Name = definition.Name,
                    Description = definition.Description,
                    SortOrder = definition.SortOrder,
                    CreatedAt = now,
                    CreatedIp = "seed"
                });
            }
            else
            {
                module.Name = definition.Name;
                module.Description = definition.Description;
                module.SortOrder = definition.SortOrder;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var email = InputRules.Email(configuration["Seed:SuperAdminEmail"] ?? "superadmin@aangan.local");
        var name = InputRules.Required(configuration["Seed:SuperAdminName"]);
        if (name.Length == 0)
            name = "Aangan Super Admin";
        var password = configuration["Seed:SuperAdminPassword"] ?? "SuperAdmin@123";

        var existing = await db.Users.AnyAsync(user => user.Email == email, cancellationToken);
        if (!existing)
        {
            var superRole = await db.Roles.FirstAsync(role => role.Name == RoleNames.SuperAdmin, cancellationToken);
            var admin = new AppUser
            {
                FullName = name,
                Email = email,
                IsSuperAdmin = true,
                IsActive = true,
                CreatedAt = now,
                CreatedIp = "seed"
            };
            admin.PasswordHash = passwords.HashPassword(admin, password);
            db.Users.Add(admin);
            db.UserRoles.Add(new UserRole
            {
                User = admin,
                Role = superRole,
                CreatedAt = now,
                CreatedIp = "seed"
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        var moduleRows = await db.Modules.ToListAsync(cancellationToken);
        var restaurants = await db.Restaurants.Select(item => item.Id).ToListAsync(cancellationToken);
        foreach (var restaurantId in restaurants)
        {
            var linked = await db.RestaurantModules
                .Where(item => item.RestaurantId == restaurantId)
                .Select(item => item.ModuleId)
                .ToListAsync(cancellationToken);

            foreach (var module in moduleRows.Where(item => !linked.Contains(item.Id)))
            {
                var definition = ModuleCodes.All.FirstOrDefault(item => item.Code == module.Code);
                db.RestaurantModules.Add(new RestaurantModule
                {
                    RestaurantId = restaurantId,
                    ModuleId = module.Id,
                    IsEnabled = definition?.EnabledByDefault ?? false,
                    CreatedAt = now,
                    CreatedIp = "seed"
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
