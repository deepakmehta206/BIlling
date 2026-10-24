namespace Billing.Application.Dtos;

public sealed record StockItemDto(Guid Id, string Name, string Unit, decimal OnHand, decimal ReorderLevel, bool Low);

public sealed record StockMovementDto(
    Guid Id,
    Guid StockItemId,
    string ItemName,
    decimal Quantity,
    decimal? UnitCost,
    string Reason,
    string? Note,
    DateTime CreatedAt);

public sealed record RecipeLineDto(Guid StockItemId, string StockItemName, string Unit, decimal Quantity);

public sealed record DishRecipeDto(Guid MenuItemId, string MenuItemName, IReadOnlyList<RecipeLineDto> Lines);

public sealed record DishOptionDto(Guid Id, string Name);

public sealed record InventoryDeskDto(
    IReadOnlyList<StockItemDto> Items,
    IReadOnlyList<StockMovementDto> Movements,
    IReadOnlyList<DishOptionDto> Dishes,
    IReadOnlyList<DishRecipeDto> Recipes);

public sealed class SaveStockItemRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "g";
    public decimal ReorderLevel { get; set; }
}

public sealed class PurchaseStockRequest
{
    public Guid StockItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? Note { get; set; }
}

public sealed class AdjustStockRequest
{
    public Guid StockItemId { get; set; }
    public decimal Quantity { get; set; }
    public string? Note { get; set; }
}

public sealed class RecipeUseRequest
{
    public Guid StockItemId { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class SaveRecipeRequest
{
    public List<RecipeUseRequest> Lines { get; set; } = [];
}
