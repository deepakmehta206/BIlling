using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class MenuCategory : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
}

public class MenuItem : AuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Guid CategoryId { get; set; }
    public MenuCategory Category { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FoodType { get; set; } = "Veg";
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int SortOrder { get; set; }
    public ICollection<ItemVariant> Variants { get; set; } = new List<ItemVariant>();
    public ICollection<ItemAddon> Addons { get; set; } = new List<ItemAddon>();
}

public class ItemVariant : AuditableEntity
{
    public Guid MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class ItemAddon : AuditableEntity
{
    public Guid MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
