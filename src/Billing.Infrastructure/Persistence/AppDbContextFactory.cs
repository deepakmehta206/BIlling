using Billing.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Persistence;

public sealed class DesignTimeRequestContext : IRequestContext
{
    public bool IsAuthenticated => false;
    public Guid? UserId => null;
    public string? Email => null;
    public bool IsSuperAdmin => false;
    public Guid? RestaurantId => null;
    public string? IpAddress => "design-time";
    public string? UserAgent => null;
}

public sealed class AppDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=BillingSoftware;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True")
            .Options;

        return new AppDbContext(options, new DesignTimeRequestContext());
    }
}
