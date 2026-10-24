using Billing.Application.Common;
using Billing.Application.Dtos;

namespace Billing.Application.Abstractions;

public interface IRestaurantService
{
    Task<ServiceResult<IReadOnlyList<RestaurantListItemDto>>> ListAsync(string? search, CancellationToken cancellationToken);
    Task<ServiceResult<RestaurantDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<ServiceResult<RestaurantDetailDto>> CreateAsync(CreateRestaurantRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<RestaurantDetailDto>> UpdateAsync(Guid id, UpdateRestaurantRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<MessageDto>> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<ServiceResult<IReadOnlyList<ModuleDto>>> UpdateModulesAsync(Guid id, UpdateModulesRequest request, CancellationToken cancellationToken);
}
