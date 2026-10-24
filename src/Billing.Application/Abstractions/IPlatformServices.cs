using Billing.Application.Common;
using Billing.Application.Dtos;

namespace Billing.Application.Abstractions;

public interface IAuditQueryService
{
    Task<ServiceResult<PagedResult<AuditLogDto>>> ListAsync(int page, int pageSize, Guid? restaurantId, string? action, CancellationToken cancellationToken);
}

public interface IDashboardService
{
    Task<ServiceResult<DashboardDto>> GetAsync(CancellationToken cancellationToken);
}

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) Create(
        Guid userId,
        string email,
        string fullName,
        bool isSuperAdmin,
        Guid? restaurantId,
        IReadOnlyList<string> roles);
}
