using Billing.Application.Abstractions;
using Billing.Application.Common;
using Billing.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Services;

public class AuditQueryService(Persistence.AppDbContext db, IRequestContext request) : IAuditQueryService
{
    public async Task<ServiceResult<PagedResult<AuditLogDto>>> ListAsync(
        int page,
        int pageSize,
        Guid? restaurantId,
        string? action,
        CancellationToken cancellationToken)
    {
        if (!request.IsAuthenticated)
            return ServiceResult<PagedResult<AuditLogDto>>.Fail(401, "Sign in again.");

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.AuditLogs.AsQueryable();
        if (!request.IsSuperAdmin)
        {
            if (request.RestaurantId is not Guid ownId)
                return ServiceResult<PagedResult<AuditLogDto>>.Fail(403, "This account is not attached to a restaurant.");

            query = query.Where(item => item.RestaurantId == ownId);
        }
        else if (restaurantId is Guid filterId)
        {
            query = query.Where(item => item.RestaurantId == filterId);
        }

        var actionFilter = InputRules.Clean(action);
        if (actionFilter is not null)
            query = query.Where(item => item.Action == actionFilter);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AuditLogDto(
                item.Id,
                item.Action,
                item.EntityName,
                item.EntityId,
                item.Description,
                item.ActorEmail,
                item.RestaurantId,
                item.RestaurantName,
                item.CreatedIp,
                item.UserAgent,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        return ServiceResult<PagedResult<AuditLogDto>>.Ok(new PagedResult<AuditLogDto>(items, page, pageSize, total));
    }
}

public class DashboardService(Persistence.AppDbContext db, IRequestContext request) : IDashboardService
{
    public async Task<ServiceResult<DashboardDto>> GetAsync(CancellationToken cancellationToken)
    {
        if (!request.IsAuthenticated)
            return ServiceResult<DashboardDto>.Fail(401, "Sign in again.");

        var restaurants = db.Restaurants.AsQueryable();
        var users = db.Users.Where(user => !user.IsSuperAdmin);
        var audits = db.AuditLogs.AsQueryable();
        var modules = db.RestaurantModules.AsQueryable();

        if (!request.IsSuperAdmin)
        {
            if (request.RestaurantId is not Guid restaurantId)
                return ServiceResult<DashboardDto>.Fail(403, "This account is not attached to a restaurant.");

            restaurants = restaurants.Where(item => item.Id == restaurantId);
            users = users.Where(item => item.RestaurantId == restaurantId);
            audits = audits.Where(item => item.RestaurantId == restaurantId);
            modules = modules.Where(item => item.RestaurantId == restaurantId);
        }

        var today = DateTime.UtcNow.Date;
        var restaurantCount = await restaurants.CountAsync(cancellationToken);
        var activeCount = await restaurants.CountAsync(item => item.IsActive, cancellationToken);
        var staffCount = await users.CountAsync(cancellationToken);
        var auditsToday = await audits.CountAsync(item => item.CreatedAt >= today, cancellationToken);

        var counted = await modules
            .GroupBy(item => item.Module.Code)
            .Select(group => new
            {
                Code = group.Key,
                Enabled = group.Count(item => item.IsEnabled),
                Total = group.Count()
            })
            .ToListAsync(cancellationToken);

        var catalog = await db.Modules
            .OrderBy(item => item.SortOrder)
            .Select(item => new { item.Code, item.Name })
            .ToListAsync(cancellationToken);

        var usage = catalog
            .Select(item =>
            {
                var count = counted.FirstOrDefault(row => row.Code == item.Code);
                return new ModuleUsageDto(item.Code, item.Name, count?.Enabled ?? 0, count?.Total ?? 0);
            })
            .ToList();

        var recent = await audits
            .OrderByDescending(item => item.CreatedAt)
            .Take(8)
            .Select(item => new AuditLogDto(
                item.Id,
                item.Action,
                item.EntityName,
                item.EntityId,
                item.Description,
                item.ActorEmail,
                item.RestaurantId,
                item.RestaurantName,
                item.CreatedIp,
                item.UserAgent,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        return ServiceResult<DashboardDto>.Ok(new DashboardDto(
            restaurantCount,
            activeCount,
            staffCount,
            auditsToday,
            usage,
            recent));
    }
}
