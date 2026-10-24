using Billing.Application.Common;
using Billing.Application.Dtos;
using Billing.Domain.Entities;

namespace Billing.Application.Abstractions;

public interface IInventoryService
{
    Task<ServiceResult<InventoryDeskDto>> GetAsync(Guid? restaurantId, CancellationToken cancellationToken);
    Task<ServiceResult<StockItemDto>> SaveItemAsync(Guid? restaurantId, SaveStockItemRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<MessageDto>> DeleteItemAsync(Guid? restaurantId, Guid itemId, CancellationToken cancellationToken);
    Task<ServiceResult<StockItemDto>> PurchaseAsync(Guid? restaurantId, PurchaseStockRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<StockItemDto>> AdjustAsync(Guid? restaurantId, AdjustStockRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<DishRecipeDto>> SaveRecipeAsync(Guid? restaurantId, Guid menuItemId, SaveRecipeRequest request, CancellationToken cancellationToken);
    Task ConsumeAsync(Guid restaurantId, string restaurantName, Bill bill, IReadOnlyList<OrderLine> lines, CancellationToken cancellationToken);
}
