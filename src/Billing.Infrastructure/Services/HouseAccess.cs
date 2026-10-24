using Billing.Application.Common;
using Billing.Domain.Security;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Services;

public sealed record HouseScope(Guid Id, string Name);

public class HouseAccess(AppDbContext db, Application.Abstractions.IRequestContext request)
{
    public Task<ServiceResult<HouseScope>> RequireMenu(Guid? restaurantId, CancellationToken cancellationToken) =>
        Require(restaurantId, ModuleCodes.Menu, cancellationToken);

    public Task<ServiceResult<HouseScope>> RequireTables(Guid? restaurantId, CancellationToken cancellationToken) =>
        Require(restaurantId, ModuleCodes.Tables, cancellationToken);

    public Task<ServiceResult<HouseScope>> RequireBooking(Guid? restaurantId, CancellationToken cancellationToken) =>
        Require(restaurantId, ModuleCodes.Booking, cancellationToken);

    public Task<ServiceResult<HouseScope>> RequireTickets(Guid? restaurantId, CancellationToken cancellationToken) =>
        RequireAny(restaurantId, [ModuleCodes.TokenDisplay, ModuleCodes.QrOrder], cancellationToken);

    public Task<ServiceResult<HouseScope>> RequireBilling(Guid? restaurantId, CancellationToken cancellationToken) =>
        Require(restaurantId, ModuleCodes.Billing, cancellationToken);

    public Task<bool> IsOn(Guid restaurantId, string module, CancellationToken cancellationToken) =>
        ModuleOn(restaurantId, module, cancellationToken);

    public async Task<ServiceResult<HouseScope>> RequirePublic(Guid restaurantId, string module, CancellationToken cancellationToken)
    {
        var restaurant = await db.Restaurants
            .Where(item => item.Id == restaurantId)
            .Select(item => new { item.Id, item.Name, item.IsActive })
            .FirstOrDefaultAsync(cancellationToken);

        if (restaurant is null || !restaurant.IsActive)
            return ServiceResult<HouseScope>.Fail(404, "This house is not taking guests.");

        var enabled = await ModuleOn(restaurant.Id, module, cancellationToken);
        return enabled
            ? ServiceResult<HouseScope>.Ok(new HouseScope(restaurant.Id, restaurant.Name))
            : ServiceResult<HouseScope>.Fail(403, "This desk is turned off for the house.");
    }

    private async Task<ServiceResult<HouseScope>> Require(Guid? restaurantId, string module, CancellationToken cancellationToken)
    {
        var house = await Resolve(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return house;

        var enabled = await ModuleOn(house.Value.Id, module, cancellationToken);
        return enabled
            ? house
            : ServiceResult<HouseScope>.Fail(403, "This desk is turned off for the house.");
    }

    private async Task<ServiceResult<HouseScope>> RequireAny(Guid? restaurantId, string[] modules, CancellationToken cancellationToken)
    {
        var house = await Resolve(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return house;

        foreach (var module in modules)
        {
            if (await ModuleOn(house.Value.Id, module, cancellationToken))
                return house;
        }

        return ServiceResult<HouseScope>.Fail(403, "This desk is turned off for the house.");
    }

    private async Task<ServiceResult<HouseScope>> Resolve(Guid? restaurantId, CancellationToken cancellationToken)
    {
        if (!request.IsAuthenticated)
            return ServiceResult<HouseScope>.Fail(401, "Sign in again.");

        Guid id;
        if (request.IsSuperAdmin)
        {
            if (restaurantId is not Guid chosen)
                return ServiceResult<HouseScope>.Fail(400, "Choose a restaurant first.");
            id = chosen;
        }
        else if (request.RestaurantId is Guid own)
        {
            id = own;
        }
        else
        {
            return ServiceResult<HouseScope>.Fail(403, "This account is not attached to a restaurant.");
        }

        var restaurant = await db.Restaurants
            .Where(item => item.Id == id)
            .Select(item => new { item.Id, item.Name, item.IsActive })
            .FirstOrDefaultAsync(cancellationToken);

        if (restaurant is null)
            return ServiceResult<HouseScope>.Fail(404, "Restaurant not found.");

        if (!restaurant.IsActive && !request.IsSuperAdmin)
            return ServiceResult<HouseScope>.Fail(403, "This restaurant is paused.");

        return ServiceResult<HouseScope>.Ok(new HouseScope(restaurant.Id, restaurant.Name));
    }

    private Task<bool> ModuleOn(Guid restaurantId, string module, CancellationToken cancellationToken) =>
        db.RestaurantModules.AnyAsync(
            item => item.RestaurantId == restaurantId && item.IsEnabled && item.Module.Code == module,
            cancellationToken);
}
