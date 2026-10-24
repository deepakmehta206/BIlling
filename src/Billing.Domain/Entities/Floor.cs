using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class DiningTable : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Section { get; set; } = "Hall";
    public int Capacity { get; set; } = 4;
    public decimal PosX { get; set; }
    public decimal PosY { get; set; }
    public string Status { get; set; } = "Available";
    public string QrToken { get; set; } = string.Empty;
    public ICollection<TableBooking> Bookings { get; set; } = new List<TableBooking>();
    public ICollection<GuestOrder> Orders { get; set; } = new List<GuestOrder>();
}

public class TableBooking : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Guid DiningTableId { get; set; }
    public DiningTable DiningTable { get; set; } = null!;
    public string TableName { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Status { get; set; } = "Booked";
    public string? Notes { get; set; }
}

public class GuestOrder : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Guid? DiningTableId { get; set; }
    public DiningTable? DiningTable { get; set; }
    public string? TableName { get; set; }
    public int TokenNumber { get; set; }
    public DateOnly ServiceDate { get; set; }
    public string Status { get; set; } = "Preparing";
    public string? GuestName { get; set; }
    public string? Note { get; set; }
    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
}

public class OrderLine : AuditableEntity
{
    public Guid GuestOrderId { get; set; }
    public GuestOrder GuestOrder { get; set; } = null!;
    public Guid? MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? AddonSummary { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Note { get; set; }
}
