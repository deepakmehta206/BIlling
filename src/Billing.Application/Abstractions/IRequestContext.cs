namespace Billing.Application.Abstractions;

public interface IRequestContext
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? Email { get; }
    bool IsSuperAdmin { get; }
    Guid? RestaurantId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
