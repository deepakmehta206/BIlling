using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class AuditLog : AuditableEntity
{
    public Guid? ActorId { get; set; }
    public string? ActorEmail { get; set; }
    public Guid? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
}
