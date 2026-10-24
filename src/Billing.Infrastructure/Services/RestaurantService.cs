using Billing.Application.Abstractions;
using Billing.Application.Common;
using Billing.Application.Dtos;
using Billing.Domain.Entities;
using Billing.Domain.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Services;

public class RestaurantService(
    Persistence.AppDbContext db,
    IRequestContext request,
    IPasswordHasher<AppUser> passwords) : IRestaurantService
{
    public async Task<ServiceResult<IReadOnlyList<RestaurantListItemDto>>> ListAsync(string? search, CancellationToken cancellationToken)
    {
        if (!request.IsSuperAdmin)
            return ServiceResult<IReadOnlyList<RestaurantListItemDto>>.Fail(403, "Only the platform admin can see every restaurant.");

        var query = db.Restaurants.AsQueryable();
        var term = InputRules.Clean(search);
        if (term is not null)
        {
            query = query.Where(item =>
                item.Name.Contains(term) ||
                item.Code.Contains(term) ||
                (item.City != null && item.City.Contains(term)) ||
                item.OwnerName.Contains(term));
        }

        var rows = await query
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new RestaurantListItemDto(
                item.Id,
                item.Name,
                item.Code,
                item.City,
                item.Phone,
                item.OwnerName,
                item.OwnerPhone,
                item.IsActive,
                item.Modules.Count(module => module.IsEnabled),
                item.Modules.Count(),
                item.Users
                    .Where(user => user.UserRoles.Any(link => link.Role.Name == RoleNames.RestaurantAdmin))
                    .OrderBy(user => user.CreatedAt)
                    .Select(user => user.Email)
                    .FirstOrDefault(),
                item.CreatedIp,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<RestaurantListItemDto>>.Ok(rows);
    }

    public async Task<ServiceResult<RestaurantDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!CanRead(id))
            return ServiceResult<RestaurantDetailDto>.Fail(403, "You can open only your own restaurant.");

        var detail = await LoadAsync(id, cancellationToken);
        return detail is null
            ? ServiceResult<RestaurantDetailDto>.Fail(404, "Restaurant not found.")
            : ServiceResult<RestaurantDetailDto>.Ok(detail);
    }

    public async Task<ServiceResult<RestaurantDetailDto>> CreateAsync(CreateRestaurantRequest body, CancellationToken cancellationToken)
    {
        if (!request.IsSuperAdmin)
            return ServiceResult<RestaurantDetailDto>.Fail(403, "Only the platform admin can open a restaurant.");

        var house = ReadHouse(body.Name, body.LegalName, body.Address, body.City, body.State, body.Phone, body.Email, body.Gstin, body.OwnerName, body.OwnerPhone, body.UpiId);
        if (house.Error is not null)
            return ServiceResult<RestaurantDetailDto>.Fail(400, house.Error);

        var adminName = InputRules.Required(body.AdminFullName);
        var adminEmail = InputRules.Email(body.AdminEmail);
        var adminPhone = InputRules.Required(body.AdminPhone);
        var adminPassword = body.AdminPassword ?? string.Empty;
        if (adminName.Length < 2)
            return ServiceResult<RestaurantDetailDto>.Fail(400, "Enter the restaurant admin's name.");
        if (!InputRules.ValidEmail(adminEmail))
            return ServiceResult<RestaurantDetailDto>.Fail(400, "Enter a valid admin email.");
        if (!InputRules.ValidPhone(adminPhone))
            return ServiceResult<RestaurantDetailDto>.Fail(400, "Admin phone needs 10 to 15 digits.");
        if (!InputRules.ValidPassword(adminPassword))
            return ServiceResult<RestaurantDetailDto>.Fail(400, "Admin password needs 8 characters, with a letter and a number.");

        if (await db.Users.AnyAsync(user => user.Email == adminEmail, cancellationToken))
            return ServiceResult<RestaurantDetailDto>.Fail(409, "That admin email is already used.");

        var role = await db.Roles.FirstOrDefaultAsync(item => item.Name == RoleNames.RestaurantAdmin, cancellationToken);
        if (role is null)
            return ServiceResult<RestaurantDetailDto>.Fail(500, "Restaurant admin role is not seeded yet.");

        var modules = await db.Modules.OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
        var code = await NextCodeAsync(InputRules.Clean(body.Code) ?? house.Name, null, cancellationToken);

        var restaurant = new Restaurant
        {
            Id = Guid.NewGuid(),
            Name = house.Name,
            Code = code,
            LegalName = house.LegalName,
            Address = house.Address,
            City = house.City,
            State = house.State,
            Phone = house.Phone,
            Email = house.Email,
            Gstin = house.Gstin,
            OwnerName = house.OwnerName,
            OwnerPhone = house.OwnerPhone,
            UpiId = house.UpiId,
            IsActive = true
        };

        var admin = new AppUser
        {
            Id = Guid.NewGuid(),
            Restaurant = restaurant,
            FullName = adminName,
            Email = adminEmail,
            Phone = adminPhone,
            IsActive = true,
            IsSuperAdmin = false
        };
        admin.PasswordHash = passwords.HashPassword(admin, adminPassword);

        db.Restaurants.Add(restaurant);
        db.Users.Add(admin);
        db.UserRoles.Add(new UserRole { User = admin, Role = role });
        foreach (var module in modules)
        {
            var definition = ModuleCodes.All.FirstOrDefault(item => item.Code == module.Code);
            db.RestaurantModules.Add(new RestaurantModule
            {
                Restaurant = restaurant,
                Module = module,
                IsEnabled = definition?.EnabledByDefault ?? false
            });
        }

        db.AuditLogs.Add(Persistence.AuditEntries.Create(
            request,
            "restaurant.created",
            "Restaurant",
            restaurant.Id.ToString(),
            $"Opened {restaurant.Name} ({restaurant.Code}).",
            restaurant.Id,
            restaurant.Name));

        await db.SaveChangesAsync(cancellationToken);

        var detail = await LoadAsync(restaurant.Id, cancellationToken);
        return ServiceResult<RestaurantDetailDto>.Ok(detail!);
    }

    public async Task<ServiceResult<RestaurantDetailDto>> UpdateAsync(Guid id, UpdateRestaurantRequest body, CancellationToken cancellationToken)
    {
        if (!request.IsSuperAdmin)
            return ServiceResult<RestaurantDetailDto>.Fail(403, "Only the platform admin can edit a restaurant.");

        var restaurant = await db.Restaurants.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (restaurant is null)
            return ServiceResult<RestaurantDetailDto>.Fail(404, "Restaurant not found.");

        var house = ReadHouse(body.Name, body.LegalName, body.Address, body.City, body.State, body.Phone, body.Email, body.Gstin, body.OwnerName, body.OwnerPhone, body.UpiId);
        if (house.Error is not null)
            return ServiceResult<RestaurantDetailDto>.Fail(400, house.Error);

        var requestedCode = InputRules.Clean(body.Code);
        restaurant.Code = await NextCodeAsync(requestedCode ?? restaurant.Code, restaurant.Id, cancellationToken);
        restaurant.Name = house.Name;
        restaurant.LegalName = house.LegalName;
        restaurant.Address = house.Address;
        restaurant.City = house.City;
        restaurant.State = house.State;
        restaurant.Phone = house.Phone;
        restaurant.Email = house.Email;
        restaurant.Gstin = house.Gstin;
        restaurant.OwnerName = house.OwnerName;
        restaurant.OwnerPhone = house.OwnerPhone;
        restaurant.UpiId = house.UpiId;
        restaurant.IsActive = body.IsActive;

        db.AuditLogs.Add(Persistence.AuditEntries.Create(
            request,
            "restaurant.updated",
            "Restaurant",
            restaurant.Id.ToString(),
            $"Updated {restaurant.Name} ({restaurant.Code}).",
            restaurant.Id,
            restaurant.Name));

        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<RestaurantDetailDto>.Ok((await LoadAsync(id, cancellationToken))!);
    }

    public async Task<ServiceResult<MessageDto>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!request.IsSuperAdmin)
            return ServiceResult<MessageDto>.Fail(403, "Only the platform admin can remove a restaurant.");

        var restaurant = await db.Restaurants.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (restaurant is null)
            return ServiceResult<MessageDto>.Fail(404, "Restaurant not found.");

        var users = await db.Users.Where(user => user.RestaurantId == id).ToListAsync(cancellationToken);
        var userIds = users.Select(user => user.Id).ToList();
        var links = await db.UserRoles.Where(link => userIds.Contains(link.UserId)).ToListAsync(cancellationToken);
        var modules = await db.RestaurantModules.Where(module => module.RestaurantId == id).ToListAsync(cancellationToken);

        foreach (var link in links)
            link.IsDeleted = true;
        foreach (var user in users)
            user.IsDeleted = true;
        foreach (var module in modules)
            module.IsDeleted = true;
        restaurant.IsDeleted = true;

        db.AuditLogs.Add(Persistence.AuditEntries.Create(
            request,
            "restaurant.deleted",
            "Restaurant",
            restaurant.Id.ToString(),
            $"Soft-deleted {restaurant.Name} ({restaurant.Code}). Nothing was removed from the database.",
            restaurant.Id,
            restaurant.Name));

        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<MessageDto>.Ok(new MessageDto($"{restaurant.Name} was archived. The record stays in the database."));
    }

    public async Task<ServiceResult<IReadOnlyList<ModuleDto>>> UpdateModulesAsync(Guid id, UpdateModulesRequest body, CancellationToken cancellationToken)
    {
        if (!request.IsSuperAdmin)
            return ServiceResult<IReadOnlyList<ModuleDto>>.Fail(403, "Only the platform admin can change modules.");

        var restaurant = await db.Restaurants.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (restaurant is null)
            return ServiceResult<IReadOnlyList<ModuleDto>>.Fail(404, "Restaurant not found.");

        var links = await db.RestaurantModules
            .Include(item => item.Module)
            .Where(item => item.RestaurantId == id)
            .ToListAsync(cancellationToken);

        var byCode = links.ToDictionary(item => item.Module.Code, StringComparer.OrdinalIgnoreCase);
        var changes = new List<string>();
        foreach (var toggle in body.Modules ?? [])
        {
            var code = InputRules.Required(toggle.Code).ToUpperInvariant();
            if (!byCode.TryGetValue(code, out var link))
                return ServiceResult<IReadOnlyList<ModuleDto>>.Fail(400, $"Unknown module {code}.");

            if (link.IsEnabled == toggle.IsEnabled)
                continue;

            link.IsEnabled = toggle.IsEnabled;
            changes.Add($"{link.Module.Name} {(toggle.IsEnabled ? "on" : "off")}");
        }

        if (changes.Count > 0)
        {
            db.AuditLogs.Add(Persistence.AuditEntries.Create(
                request,
                "restaurant.modules_updated",
                "Restaurant",
                restaurant.Id.ToString(),
                $"Modules for {restaurant.Name}: {string.Join(", ", changes)}.",
                restaurant.Id,
                restaurant.Name));
            await db.SaveChangesAsync(cancellationToken);
        }

        var detail = await LoadAsync(id, cancellationToken);
        return ServiceResult<IReadOnlyList<ModuleDto>>.Ok(detail!.Modules);
    }

    private bool CanRead(Guid id) => request.IsSuperAdmin || request.RestaurantId == id;

    private async Task<string> NextCodeAsync(string preferred, Guid? exceptId, CancellationToken cancellationToken)
    {
        var root = Persistence.RestaurantCodes.ToCode(preferred);
        var code = root;
        var suffix = 2;
        while (await db.Restaurants.AnyAsync(item => item.Code == code && item.Id != exceptId, cancellationToken))
        {
            var tail = "-" + suffix;
            var head = root.Length + tail.Length > 24 ? root[..(24 - tail.Length)].Trim('-') : root;
            code = head + tail;
            suffix++;
        }

        return code;
    }

    private async Task<RestaurantDetailDto?> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        var restaurant = await db.Restaurants.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (restaurant is null)
            return null;

        var admin = await db.Users
            .Where(user => user.RestaurantId == id && user.UserRoles.Any(link => link.Role.Name == RoleNames.RestaurantAdmin))
            .OrderBy(user => user.CreatedAt)
            .Select(user => new AdminUserDto(user.Id, user.FullName, user.Email, user.Phone, user.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        var modules = await db.RestaurantModules
            .Where(item => item.RestaurantId == id)
            .OrderBy(item => item.Module.SortOrder)
            .Select(item => new ModuleDto(item.Module.Code, item.Module.Name, item.Module.Description, item.IsEnabled, item.Module.SortOrder))
            .ToListAsync(cancellationToken);

        return new RestaurantDetailDto(
            restaurant.Id,
            restaurant.Name,
            restaurant.Code,
            restaurant.LegalName,
            restaurant.Address,
            restaurant.City,
            restaurant.State,
            restaurant.Phone,
            restaurant.Email,
            restaurant.Gstin,
            restaurant.OwnerName,
            restaurant.OwnerPhone,
            restaurant.UpiId,
            restaurant.IsActive,
            restaurant.CreatedAt,
            restaurant.CreatedIp,
            restaurant.UpdatedAt,
            restaurant.UpdatedIp,
            admin,
            modules);
    }

    private static House ReadHouse(
        string? name,
        string? legalName,
        string? address,
        string? city,
        string? state,
        string? phone,
        string? email,
        string? gstin,
        string? ownerName,
        string? ownerPhone,
        string? upiId)
    {
        var cleanName = InputRules.Required(name);
        var cleanOwner = InputRules.Required(ownerName);
        var cleanOwnerPhone = InputRules.Required(ownerPhone);
        if (cleanName.Length < 2)
            return House.Invalid("Restaurant name needs at least 2 characters.");
        if (cleanOwner.Length < 2)
            return House.Invalid("Enter the owner's name.");
        if (!InputRules.ValidPhone(cleanOwnerPhone))
            return House.Invalid("Owner phone needs 10 to 15 digits.");

        var cleanEmail = InputRules.Email(email);
        if (cleanEmail.Length > 0 && !InputRules.ValidEmail(cleanEmail))
            return House.Invalid("Enter a valid restaurant email, or leave it blank.");

        var cleanPhone = InputRules.Clean(phone);
        if (cleanPhone is not null && !InputRules.ValidPhone(cleanPhone))
            return House.Invalid("Restaurant phone needs 10 to 15 digits.");

        var cleanGstin = InputRules.Clean(gstin)?.ToUpperInvariant();
        if (!InputRules.ValidGstin(cleanGstin))
            return House.Invalid("GSTIN must be 15 letters or digits.");

        var cleanUpi = InputRules.Clean(upiId);
        if (!InputRules.ValidUpi(cleanUpi))
            return House.Invalid("UPI id should look like name@bank.");

        return new House(
            cleanName,
            InputRules.Clean(legalName),
            InputRules.Clean(address),
            InputRules.Clean(city),
            InputRules.Clean(state),
            cleanPhone,
            cleanEmail.Length == 0 ? null : cleanEmail,
            cleanGstin,
            cleanOwner,
            cleanOwnerPhone,
            cleanUpi,
            null);
    }

    private sealed record House(
        string Name,
        string? LegalName,
        string? Address,
        string? City,
        string? State,
        string? Phone,
        string? Email,
        string? Gstin,
        string OwnerName,
        string OwnerPhone,
        string? UpiId,
        string? Error)
    {
        public static House Invalid(string error) => new("", null, null, null, null, null, null, null, "", "", null, error);
    }
}
