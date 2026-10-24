using Billing.Application.Abstractions;
using Billing.Application.Common;
using Billing.Application.Dtos;
using Billing.Domain.Entities;
using Billing.Domain.Inventory;
using Billing.Domain.Security;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Services;

public class InventoryService(AppDbContext db, HouseAccess access, IRequestContext request, IHouseLive live) : IInventoryService
{
    public async Task<ServiceResult<InventoryDeskDto>> GetAsync(Guid? restaurantId, CancellationToken cancellationToken)
    {
        var house = await access.RequireInventory(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<InventoryDeskDto>.Fail(house.StatusCode, house.Error ?? "Cannot open the store.");

        var items = await db.StockItems
            .Where(item => item.RestaurantId == house.Value.Id)
            .OrderBy(item => item.Name)
            .Select(item => new StockItemDto(item.Id, item.Name, item.Unit, item.OnHand, item.ReorderLevel, item.OnHand <= item.ReorderLevel))
            .ToListAsync(cancellationToken);

        var movements = await db.StockMovements
            .Where(item => item.RestaurantId == house.Value.Id)
            .OrderByDescending(item => item.CreatedAt)
            .Take(40)
            .Select(item => new StockMovementDto(item.Id, item.StockItemId, item.StockItem.Name, item.Quantity, item.UnitCost, item.Reason, item.Note, item.CreatedAt))
            .ToListAsync(cancellationToken);

        var dishes = await db.MenuItems
            .Where(item => item.RestaurantId == house.Value.Id)
            .OrderBy(item => item.Name)
            .Select(item => new DishOptionDto(item.Id, item.Name))
            .ToListAsync(cancellationToken);

        var recipeRows = await db.RecipeLines
            .Where(item => item.RestaurantId == house.Value.Id)
            .Select(item => new { item.MenuItemId, item.MenuItem.Name, item.StockItemId, StockName = item.StockItem.Name, item.StockItem.Unit, item.Quantity })
            .ToListAsync(cancellationToken);

        var recipes = recipeRows
            .GroupBy(item => new { item.MenuItemId, item.Name })
            .Select(group => new DishRecipeDto(
                group.Key.MenuItemId,
                group.Key.Name,
                group.Select(line => new RecipeLineDto(line.StockItemId, line.StockName, line.Unit, line.Quantity)).ToList()))
            .OrderBy(item => item.MenuItemName)
            .ToList();

        return ServiceResult<InventoryDeskDto>.Ok(new InventoryDeskDto(items, movements, dishes, recipes));
    }

    public async Task<ServiceResult<StockItemDto>> SaveItemAsync(Guid? restaurantId, SaveStockItemRequest body, CancellationToken cancellationToken)
    {
        var house = await access.RequireInventory(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<StockItemDto>.Fail(house.StatusCode, house.Error ?? "Cannot save stock.");

        var name = InputRules.Required(body.Name);
        if (name.Length is < 2 or > 80)
            return ServiceResult<StockItemDto>.Fail(400, "Name needs 2 to 80 characters.");

        if (!StockUnits.Known(body.Unit))
            return ServiceResult<StockItemDto>.Fail(400, "Unit must be g, kg, ml, L or pcs.");

        if (body.ReorderLevel < 0)
            return ServiceResult<StockItemDto>.Fail(400, "Reorder level cannot be negative.");

        var duplicate = await db.StockItems.AnyAsync(
            item => item.RestaurantId == house.Value.Id && item.Name == name && item.Id != body.Id,
            cancellationToken);
        if (duplicate)
            return ServiceResult<StockItemDto>.Fail(409, "That ingredient is already in the store.");

        StockItem item;
        string action;
        if (body.Id is Guid id)
        {
            var existing = await db.StockItems.FirstOrDefaultAsync(row => row.Id == id && row.RestaurantId == house.Value.Id, cancellationToken);
            if (existing is null)
                return ServiceResult<StockItemDto>.Fail(404, "Ingredient not found.");
            existing.Name = name;
            existing.Unit = body.Unit;
            existing.ReorderLevel = body.ReorderLevel;
            item = existing;
            action = "inventory.item_updated";
        }
        else
        {
            item = new StockItem
            {
                Id = Guid.NewGuid(),
                RestaurantId = house.Value.Id,
                Name = name,
                Unit = body.Unit,
                ReorderLevel = body.ReorderLevel,
            };
            db.StockItems.Add(item);
            action = "inventory.item_created";
        }

        db.AuditLogs.Add(AuditEntries.Create(request, action, "StockItem", item.Id.ToString(), $"{item.Name} ({item.Unit}).", house.Value.Id, house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<StockItemDto>.Ok(MapItem(item));
    }

    public async Task<ServiceResult<MessageDto>> DeleteItemAsync(Guid? restaurantId, Guid itemId, CancellationToken cancellationToken)
    {
        var house = await access.RequireInventory(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<MessageDto>.Fail(house.StatusCode, house.Error ?? "Cannot archive stock.");

        var item = await db.StockItems.FirstOrDefaultAsync(row => row.Id == itemId && row.RestaurantId == house.Value.Id, cancellationToken);
        if (item is null)
            return ServiceResult<MessageDto>.Fail(404, "Ingredient not found.");

        foreach (var line in await db.RecipeLines.Where(row => row.StockItemId == item.Id).ToListAsync(cancellationToken))
            db.RecipeLines.Remove(line);

        db.StockItems.Remove(item);
        db.AuditLogs.Add(AuditEntries.Create(request, "inventory.item_archived", "StockItem", item.Id.ToString(), $"{item.Name} left the store.", house.Value.Id, house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<MessageDto>.Ok(new MessageDto($"{item.Name} archived."));
    }

    public async Task<ServiceResult<StockItemDto>> PurchaseAsync(Guid? restaurantId, PurchaseStockRequest body, CancellationToken cancellationToken)
    {
        var house = await access.RequireInventory(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<StockItemDto>.Fail(house.StatusCode, house.Error ?? "Cannot receive stock.");

        if (body.Quantity <= 0)
            return ServiceResult<StockItemDto>.Fail(400, "Purchase quantity must be greater than zero.");

        if (body.UnitCost < 0)
            return ServiceResult<StockItemDto>.Fail(400, "Cost cannot be negative.");

        var item = await db.StockItems.FirstOrDefaultAsync(row => row.Id == body.StockItemId && row.RestaurantId == house.Value.Id, cancellationToken);
        if (item is null)
            return ServiceResult<StockItemDto>.Fail(404, "Ingredient not found.");

        item.OnHand += body.Quantity;
        db.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            RestaurantId = house.Value.Id,
            StockItemId = item.Id,
            Quantity = body.Quantity,
            UnitCost = body.UnitCost,
            Reason = StockReasons.Purchase,
            Note = InputRules.TrimTo(body.Note, 200),
        });
        db.AuditLogs.Add(AuditEntries.Create(request, "inventory.purchased", "StockItem", item.Id.ToString(), $"Received {body.Quantity:0.###} {item.Unit} of {item.Name}.", house.Value.Id, house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<StockItemDto>.Ok(MapItem(item));
    }

    public async Task<ServiceResult<StockItemDto>> AdjustAsync(Guid? restaurantId, AdjustStockRequest body, CancellationToken cancellationToken)
    {
        var house = await access.RequireInventory(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<StockItemDto>.Fail(house.StatusCode, house.Error ?? "Cannot adjust stock.");

        if (body.Quantity == 0)
            return ServiceResult<StockItemDto>.Fail(400, "Correction cannot be zero.");

        var item = await db.StockItems.FirstOrDefaultAsync(row => row.Id == body.StockItemId && row.RestaurantId == house.Value.Id, cancellationToken);
        if (item is null)
            return ServiceResult<StockItemDto>.Fail(404, "Ingredient not found.");

        item.OnHand += body.Quantity;
        db.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            RestaurantId = house.Value.Id,
            StockItemId = item.Id,
            Quantity = body.Quantity,
            Reason = StockReasons.Adjust,
            Note = InputRules.TrimTo(body.Note, 200),
        });
        db.AuditLogs.Add(AuditEntries.Create(request, "inventory.adjusted", "StockItem", item.Id.ToString(), $"Corrected {item.Name} by {body.Quantity:0.###} {item.Unit}.", house.Value.Id, house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<StockItemDto>.Ok(MapItem(item));
    }

    public async Task<ServiceResult<DishRecipeDto>> SaveRecipeAsync(Guid? restaurantId, Guid menuItemId, SaveRecipeRequest body, CancellationToken cancellationToken)
    {
        var house = await access.RequireInventory(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<DishRecipeDto>.Fail(house.StatusCode, house.Error ?? "Cannot save a recipe.");

        var dish = await db.MenuItems.FirstOrDefaultAsync(item => item.Id == menuItemId && item.RestaurantId == house.Value.Id, cancellationToken);
        if (dish is null)
            return ServiceResult<DishRecipeDto>.Fail(404, "Dish not found.");

        if (body.Lines.Any(line => line.Quantity <= 0))
            return ServiceResult<DishRecipeDto>.Fail(400, "Each ingredient needs a quantity above zero.");

        if (body.Lines.GroupBy(line => line.StockItemId).Any(group => group.Count() > 1))
            return ServiceResult<DishRecipeDto>.Fail(400, "An ingredient can appear once in a recipe.");

        var stockIds = body.Lines.Select(line => line.StockItemId).ToList();
        var stock = await db.StockItems.Where(item => item.RestaurantId == house.Value.Id && stockIds.Contains(item.Id)).ToListAsync(cancellationToken);
        if (stock.Count != stockIds.Count)
            return ServiceResult<DishRecipeDto>.Fail(400, "Recipe uses an ingredient that is not in this store.");

        foreach (var line in await db.RecipeLines.Where(item => item.MenuItemId == dish.Id).ToListAsync(cancellationToken))
            db.RecipeLines.Remove(line);

        var saved = new List<RecipeLineDto>();
        foreach (var line in body.Lines)
        {
            var ingredient = stock.First(item => item.Id == line.StockItemId);
            db.RecipeLines.Add(new RecipeLine
            {
                Id = Guid.NewGuid(),
                RestaurantId = house.Value.Id,
                MenuItemId = dish.Id,
                StockItemId = ingredient.Id,
                Quantity = line.Quantity,
            });
            saved.Add(new RecipeLineDto(ingredient.Id, ingredient.Name, ingredient.Unit, line.Quantity));
        }

        db.AuditLogs.Add(AuditEntries.Create(request, "inventory.recipe_saved", "MenuItem", dish.Id.ToString(), $"Recipe for {dish.Name} has {saved.Count} ingredients.", house.Value.Id, house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<DishRecipeDto>.Ok(new DishRecipeDto(dish.Id, dish.Name, saved));
    }

    public async Task ConsumeAsync(Guid restaurantId, string restaurantName, Bill bill, IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken)
    {
        if (!await access.IsOn(restaurantId, ModuleCodes.Inventory, cancellationToken))
            return;

        var demand = lines
            .Where(line => line.MenuItemId is not null && line.Quantity > 0)
            .GroupBy(line => line.MenuItemId!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(line => line.Quantity));
        if (demand.Count == 0)
            return;

        var ids = demand.Keys.ToList();
        var recipes = await db.RecipeLines
            .Where(line => line.RestaurantId == restaurantId && ids.Contains(line.MenuItemId))
            .ToListAsync(cancellationToken);
        if (recipes.Count == 0)
            return;

        var used = new Dictionary<Guid, decimal>();
        foreach (var recipe in recipes)
        {
            var plates = demand[recipe.MenuItemId];
            used[recipe.StockItemId] = used.GetValueOrDefault(recipe.StockItemId) + recipe.Quantity * plates;
        }

        var stockIds = used.Keys.ToList();
        var items = await db.StockItems.Where(item => stockIds.Contains(item.Id)).ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            var quantity = used[item.Id];
            item.OnHand -= quantity;
            db.StockMovements.Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                RestaurantId = restaurantId,
                StockItem = item,
                Bill = bill,
                Quantity = -quantity,
                Reason = StockReasons.Sale,
                Note = $"Bill {bill.BillNumber}",
            });
        }

        db.AuditLogs.Add(AuditEntries.Create(
            request,
            "inventory.consumed",
            "Bill",
            bill.Id.ToString(),
            $"Bill {bill.BillNumber} used {items.Count} ingredients.",
            restaurantId,
            restaurantName));
    }

    private static StockItemDto MapItem(StockItem item) =>
        new(item.Id, item.Name, item.Unit, item.OnHand, item.ReorderLevel, item.OnHand <= item.ReorderLevel);
}
