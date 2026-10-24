using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class UserRole : AuditableEntity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
