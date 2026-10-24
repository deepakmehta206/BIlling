using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class AppModule : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public ICollection<RestaurantModule> Restaurants { get; set; } = new List<RestaurantModule>();
}
