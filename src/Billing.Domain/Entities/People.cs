using Billing.Domain.Common;
using Billing.Domain.Hrms;

namespace Billing.Domain.Entities;

public class Shift : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
    public int GraceMinutes { get; set; }
}

public class ShiftAssignment : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
}

public class LeaveRequest : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string Kind { get; set; } = LeaveKinds.Casual;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string Status { get; set; } = LeaveStatuses.Pending;
    public string? Note { get; set; }
}

public class AttendanceMark : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public DateOnly ServiceDate { get; set; }
    public DateTime CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public string? ShiftName { get; set; }
    public bool Late { get; set; }
}
