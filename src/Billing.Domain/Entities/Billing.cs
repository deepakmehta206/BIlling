using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class Bill : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public Guid? GuestOrderId { get; set; }
    public GuestOrder? GuestOrder { get; set; }
    public int BillNumber { get; set; }
    public DateOnly ServiceDate { get; set; }
    public string? TableName { get; set; }
    public string? GuestName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal GstPercent { get; set; }
    public decimal GstAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Unpaid";
    public string PaymentMode { get; set; } = "Cash";
    public string? UpiUri { get; set; }
    public ICollection<BillLine> Lines { get; set; } = new List<BillLine>();
    public ICollection<OwnerNotice> Notices { get; set; } = new List<OwnerNotice>();
}

public class BillLine : AuditableEntity
{
    public Guid BillId { get; set; }
    public Bill Bill { get; set; } = null!;
    public string ItemName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class OwnerNotice : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Guid BillId { get; set; }
    public Bill Bill { get; set; } = null!;
    public string Phone { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "Prepared";
    public string? WaUrl { get; set; }
}
