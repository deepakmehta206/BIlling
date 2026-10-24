using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class AppUser : AuditableEntity
{
    public Guid? RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsSuperAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
