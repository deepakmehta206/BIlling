using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class StockItem : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "g";
    public decimal OnHand { get; set; }
    public decimal ReorderLevel { get; set; }
    public ICollection<RecipeLine> RecipeLines { get; set; } = new List<RecipeLine>();
    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
}

public class RecipeLine : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Guid MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public Guid StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;
    public decimal Quantity { get; set; }
}

public class StockMovement : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Guid StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;
    public Guid? BillId { get; set; }
    public Bill? Bill { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string Reason { get; set; } = "Purchase";
    public string? Note { get; set; }
}
