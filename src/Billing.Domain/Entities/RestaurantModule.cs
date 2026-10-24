using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class RestaurantModule : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public Guid ModuleId { get; set; }
    public AppModule Module { get; set; } = null!;
    public bool IsEnabled { get; set; }
}
