using Billing.Application.Abstractions;
using Billing.Application.Common;
using Billing.Application.Dtos;
using Billing.Domain.Entities;
using Billing.Domain.Floor;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Billing.Infrastructure.Services;

public class MenuService(AppDbContext db, IRequestContext request, HouseAccess access, IHouseLive live, IHostEnvironment env) : IMenuService
{
    public async Task<ServiceResult<MenuBoardDto>> GetAsync(Guid? restaurantId, CancellationToken cancellationToken)
    {
        var house = await access.RequireMenu(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<MenuBoardDto>.Fail(house.StatusCode, house.Error ?? "Cannot open the menu.");

        var categories = await db.MenuCategories
            .Where(item => item.RestaurantId == house.Value.Id)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .Select(item => new MenuCategoryDto(item.Id, item.Name, item.SortOrder, item.IsActive, item.Items.Count()))
            .ToListAsync(cancellationToken);

        var rows = await db.MenuItems
            .Where(item => item.RestaurantId == house.Value.Id)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .Select(item => new
            {
                item.Id,
                item.CategoryId,
                CategoryName = item.Category.Name,
                item.Name,
                item.Description,
                item.FoodType,
                item.Price,
                item.IsAvailable,
                item.SortOrder,
                item.ImagePath,
                Stamp = item.UpdatedAt ?? item.CreatedAt,
                Variants = item.Variants.OrderBy(variant => variant.Price).Select(variant => new NamedPrice(variant.Id, variant.Name, variant.Price)).ToList(),
                Addons = item.Addons.OrderBy(addon => addon.Name).Select(addon => new NamedPrice(addon.Id, addon.Name, addon.Price)).ToList()
            })
            .ToListAsync(cancellationToken);
        var items = rows.Select(item => new MenuItemDto(
            item.Id,
            item.CategoryId,
            item.CategoryName,
            item.Name,
            item.Description,
            item.FoodType,
            item.Price,
            item.IsAvailable,
            item.SortOrder,
            Photo(item.ImagePath, item.Stamp),
            item.Variants,
            item.Addons)).ToList();

        return ServiceResult<MenuBoardDto>.Ok(new MenuBoardDto(categories, items));
    }

    public async Task<ServiceResult<MenuCategoryDto>> SaveCategoryAsync(Guid? restaurantId, SaveCategoryRequest body, CancellationToken cancellationToken)
    {
        var house = await access.RequireMenu(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<MenuCategoryDto>.Fail(house.StatusCode, house.Error ?? "Cannot edit the menu.");

        var name = InputRules.Required(body.Name);
        if (name.Length < 2)
            return ServiceResult<MenuCategoryDto>.Fail(400, "Category name needs at least 2 characters.");

        var duplicate = await db.MenuCategories.AnyAsync(
            item => item.RestaurantId == house.Value.Id && item.Name == name && item.Id != body.Id,
            cancellationToken);
        if (duplicate)
            return ServiceResult<MenuCategoryDto>.Fail(409, "That category already exists.");

        MenuCategory category;
        if (body.Id is Guid id)
        {
            category = await db.MenuCategories.FirstOrDefaultAsync(item => item.Id == id && item.RestaurantId == house.Value.Id, cancellationToken)
                ?? null!;
            if (category is null)
                return ServiceResult<MenuCategoryDto>.Fail(404, "Category not found.");
        }
        else
        {
            category = new MenuCategory { Id = Guid.NewGuid(), RestaurantId = house.Value.Id };
            db.MenuCategories.Add(category);
        }

        category.Name = name;
        category.SortOrder = body.SortOrder;
        category.IsActive = body.IsActive;
        db.AuditLogs.Add(AuditEntries.Create(request, body.Id is null ? "menu.category_created" : "menu.category_updated", "MenuCategory", category.Id.ToString(), $"{name} on the menu.", house.Value.Id, house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        var count = await db.MenuItems.CountAsync(item => item.CategoryId == category.Id, cancellationToken);
        return ServiceResult<MenuCategoryDto>.Ok(new MenuCategoryDto(category.Id, category.Name, category.SortOrder, category.IsActive, count));
    }

    public async Task<ServiceResult<MessageDto>> DeleteCategoryAsync(Guid? restaurantId, Guid categoryId, CancellationToken cancellationToken)
    {
        var house = await access.RequireMenu(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<MessageDto>.Fail(house.StatusCode, house.Error ?? "Cannot edit the menu.");

        var category = await db.MenuCategories.FirstOrDefaultAsync(item => item.Id == categoryId && item.RestaurantId == house.Value.Id, cancellationToken);
        if (category is null)
            return ServiceResult<MessageDto>.Fail(404, "Category not found.");

        var items = await db.MenuItems.Where(item => item.CategoryId == category.Id).ToListAsync(cancellationToken);
        var itemIds = items.Select(item => item.Id).ToList();
        var variants = await db.ItemVariants.Where(item => itemIds.Contains(item.MenuItemId)).ToListAsync(cancellationToken);
        var addons = await db.ItemAddons.Where(item => itemIds.Contains(item.MenuItemId)).ToListAsync(cancellationToken);
        foreach (var variant in variants)
            variant.IsDeleted = true;
        foreach (var addon in addons)
            addon.IsDeleted = true;
        foreach (var item in items)
            item.IsDeleted = true;
        category.IsDeleted = true;
        db.AuditLogs.Add(AuditEntries.Create(request, "menu.category_deleted", "MenuCategory", category.Id.ToString(), $"Archived category {category.Name}.", house.Value.Id, house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<MessageDto>.Ok(new MessageDto($"{category.Name} was archived."));
    }

    public async Task<ServiceResult<MenuItemDto>> SaveItemAsync(Guid? restaurantId, SaveItemRequest body, CancellationToken cancellationToken)
    {
        var house = await access.RequireMenu(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<MenuItemDto>.Fail(house.StatusCode, house.Error ?? "Cannot edit the menu.");

        var name = InputRules.Required(body.Name);
        var food = InputRules.Required(body.FoodType);
        if (name.Length < 2)
            return ServiceResult<MenuItemDto>.Fail(400, "Dish name needs at least 2 characters.");
        if (!FoodTypes.Known(food))
            return ServiceResult<MenuItemDto>.Fail(400, "Choose Veg, NonVeg or Egg.");
        if (!MenuPrice(body.Price))
            return ServiceResult<MenuItemDto>.Fail(400, "Price can have 4 digits and 2 decimals, up to 9999.99.");

        var category = await db.MenuCategories.FirstOrDefaultAsync(item => item.Id == body.CategoryId && item.RestaurantId == house.Value.Id, cancellationToken);
        if (category is null)
            return ServiceResult<MenuItemDto>.Fail(400, "Choose a category.");

        var priceError = PriceError(body.Variants) ?? PriceError(body.Addons);
        if (priceError is not null)
            return ServiceResult<MenuItemDto>.Fail(400, priceError);

        MenuItem item;
        if (body.Id is Guid id)
        {
            item = await db.MenuItems.FirstOrDefaultAsync(row => row.Id == id && row.RestaurantId == house.Value.Id, cancellationToken) ?? null!;
            if (item is null)
                return ServiceResult<MenuItemDto>.Fail(404, "Dish not found.");
            foreach (var variant in await db.ItemVariants.Where(row => row.MenuItemId == item.Id).ToListAsync(cancellationToken))
                variant.IsDeleted = true;
            foreach (var addon in await db.ItemAddons.Where(row => row.MenuItemId == item.Id).ToListAsync(cancellationToken))
                addon.IsDeleted = true;
        }
        else
        {
            item = new MenuItem { Id = Guid.NewGuid(), RestaurantId = house.Value.Id };
            db.MenuItems.Add(item);
        }

        item.CategoryId = category.Id;
        item.Name = name;
        item.Description = InputRules.Clean(body.Description);
        item.FoodType = food;
        item.Price = body.Price;
        item.IsAvailable = body.IsAvailable;
        item.SortOrder = body.SortOrder;
        var image = StoreImage(item.Id, body.ImageData, body.RemoveImage, item.ImagePath);
        if (!image.Succeeded)
            return ServiceResult<MenuItemDto>.Fail(400, image.Error ?? "The photo could not be saved.");
        item.ImagePath = image.Path;

        foreach (var variant in body.Variants ?? [])
        {
            var variantName = InputRules.Required(variant.Name);
            if (variantName.Length == 0)
                continue;
            db.ItemVariants.Add(new ItemVariant { Id = Guid.NewGuid(), MenuItemId = item.Id, Name = variantName, Price = variant.Price });
        }

        foreach (var addon in body.Addons ?? [])
        {
            var addonName = InputRules.Required(addon.Name);
            if (addonName.Length == 0)
                continue;
            db.ItemAddons.Add(new ItemAddon { Id = Guid.NewGuid(), MenuItemId = item.Id, Name = addonName, Price = addon.Price });
        }

        db.AuditLogs.Add(AuditEntries.Create(request, body.Id is null ? "menu.item_created" : "menu.item_updated", "MenuItem", item.Id.ToString(), $"{name} at {body.Price:0.00}.", house.Value.Id, house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<MenuItemDto>.Ok(await LoadItem(item.Id, cancellationToken));
    }

    public async Task<ServiceResult<MessageDto>> DeleteItemAsync(Guid? restaurantId, Guid itemId, CancellationToken cancellationToken)
    {
        var house = await access.RequireMenu(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<MessageDto>.Fail(house.StatusCode, house.Error ?? "Cannot edit the menu.");

        var item = await db.MenuItems.FirstOrDefaultAsync(row => row.Id == itemId && row.RestaurantId == house.Value.Id, cancellationToken);
        if (item is null)
            return ServiceResult<MessageDto>.Fail(404, "Dish not found.");

        foreach (var variant in await db.ItemVariants.Where(row => row.MenuItemId == item.Id).ToListAsync(cancellationToken))
            variant.IsDeleted = true;
        foreach (var addon in await db.ItemAddons.Where(row => row.MenuItemId == item.Id).ToListAsync(cancellationToken))
            addon.IsDeleted = true;
        item.IsDeleted = true;
        db.AuditLogs.Add(AuditEntries.Create(request, "menu.item_deleted", "MenuItem", item.Id.ToString(), $"Archived {item.Name}.", house.Value.Id, house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<MessageDto>.Ok(new MessageDto($"{item.Name} was archived."));
    }

    private async Task<MenuItemDto> LoadItem(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.MenuItems.Where(row => row.Id == id).Select(row => new
        {
            row.Id,
            row.CategoryId,
            CategoryName = row.Category.Name,
            row.Name,
            row.Description,
            row.FoodType,
            row.Price,
            row.IsAvailable,
            row.SortOrder,
            row.ImagePath,
            Stamp = row.UpdatedAt ?? row.CreatedAt,
            Variants = row.Variants.OrderBy(variant => variant.Price).Select(variant => new NamedPrice(variant.Id, variant.Name, variant.Price)).ToList(),
            Addons = row.Addons.OrderBy(addon => addon.Name).Select(addon => new NamedPrice(addon.Id, addon.Name, addon.Price)).ToList()
        }).FirstAsync(cancellationToken);

        return new MenuItemDto(
            item.Id,
            item.CategoryId,
            item.CategoryName,
            item.Name,
            item.Description,
            item.FoodType,
            item.Price,
            item.IsAvailable,
            item.SortOrder,
            Photo(item.ImagePath, item.Stamp),
            item.Variants,
            item.Addons);
    }

    private static string? PriceError(IEnumerable<NamedPrice>? rows)
    {
        foreach (var row in rows ?? [])
        {
            if (string.IsNullOrWhiteSpace(row.Name))
                continue;
            if (!MenuPrice(row.Price))
                return "A size or add-on price can have 4 digits and 2 decimals, up to 9999.99.";
        }

        return null;
    }

    private static bool MenuPrice(decimal value)
    {
        if (value < 0 || value > 9999.99m)
            return false;

        var scale = (decimal.GetBits(value)[3] >> 16) & 0x7F;
        return scale <= 2;
    }

    private ImageSave StoreImage(Guid itemId, string? data, bool remove, string? current)
    {
        if (remove && string.IsNullOrWhiteSpace(data))
        {
            DeleteImage(current);
            return new ImageSave(true, null, null);
        }

        if (string.IsNullOrWhiteSpace(data))
            return new ImageSave(true, current, null);

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(data);
        }
        catch (FormatException)
        {
            return new ImageSave(false, current, "The photo could not be read.");
        }

        if (bytes.Length is < 32 or > 1_200_000 || bytes[0] != 0xFF || bytes[1] != 0xD8)
            return new ImageSave(false, current, "Use a JPEG photo under 1 MB.");

        var folder = Path.Combine(env.ContentRootPath, "menu-images");
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, $"{itemId:N}.jpg"), bytes);
        return new ImageSave(true, $"/menu/{itemId:N}.jpg", null);
    }

    private void DeleteImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("/menu/", StringComparison.Ordinal))
            return;

        var name = Path.GetFileName(path.Split('?')[0]);
        var file = Path.Combine(env.ContentRootPath, "menu-images", name);
        if (File.Exists(file))
            File.Delete(file);
    }

    private static string? Photo(string? path, DateTime stamp) =>
        path is null ? null : $"{path}?v={new DateTimeOffset(DateTime.SpecifyKind(stamp, DateTimeKind.Utc)).ToUnixTimeSeconds()}";

    private sealed record ImageSave(bool Succeeded, string? Path, string? Error);
}
