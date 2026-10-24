using Billing.Application.Abstractions;
using Billing.Domain.Entities;
using Billing.Infrastructure.Persistence;
using Billing.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Billing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Billing")));

        services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRestaurantService, RestaurantService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddHostedService<DatabaseInitializer>();
        return services;
    }
}
