namespace Billing.Application.Dtos;

public sealed record AuditLogDto(
    Guid Id,
    string Action,
    string EntityName,
    string? EntityId,
    string Description,
    string? ActorEmail,
    Guid? RestaurantId,
    string? RestaurantName,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAt);

public sealed record ModuleUsageDto(string Code, string Name, int EnabledCount, int RestaurantCount);

public sealed record DashboardDto(
    int Restaurants,
    int ActiveRestaurants,
    int Staff,
    int AuditsToday,
    IReadOnlyList<ModuleUsageDto> Modules,
    IReadOnlyList<AuditLogDto> Recent);
