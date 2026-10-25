using Billing.Application.Common;
using Billing.Application.Dtos;

namespace Billing.Application.Abstractions;

public interface IMenuService
{
    Task<ServiceResult<MenuBoardDto>> GetAsync(Guid? restaurantId, CancellationToken cancellationToken);
    Task<ServiceResult<MenuCategoryDto>> SaveCategoryAsync(Guid? restaurantId, SaveCategoryRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<MessageDto>> DeleteCategoryAsync(Guid? restaurantId, Guid categoryId, CancellationToken cancellationToken);
    Task<ServiceResult<MenuItemDto>> SaveItemAsync(Guid? restaurantId, SaveItemRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<MessageDto>> DeleteItemAsync(Guid? restaurantId, Guid itemId, CancellationToken cancellationToken);
}

public interface IFloorService
{
    Task<ServiceResult<IReadOnlyList<TableDto>>> GetTablesAsync(Guid? restaurantId, DateTime? at, CancellationToken cancellationToken);
    Task<ServiceResult<TableDto>> SaveTableAsync(Guid? restaurantId, SaveTableRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<MessageDto>> DeleteTableAsync(Guid? restaurantId, Guid tableId, CancellationToken cancellationToken);
    Task<ServiceResult<IReadOnlyList<BookingDto>>> GetBookingsAsync(Guid? restaurantId, DateOnly? day, CancellationToken cancellationToken);
    Task<ServiceResult<BookingDto>> CreateBookingAsync(Guid? restaurantId, CreateBookingRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<BookingDto>> CancelBookingAsync(Guid? restaurantId, Guid bookingId, CancellationToken cancellationToken);
    Task<ServiceResult<IReadOnlyList<TicketDto>>> GetTicketsAsync(Guid? restaurantId, CancellationToken cancellationToken);
    Task<ServiceResult<TicketDto>> SetOrderStatusAsync(Guid? restaurantId, Guid orderId, SetOrderStatusRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<PublicTableDto>> GetPublicTableAsync(string qrToken, CancellationToken cancellationToken);
    Task<ServiceResult<PublicOrderDto>> PlacePublicOrderAsync(string qrToken, PlaceOrderRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<PublicOrderDto>> GetPublicOrderAsync(Guid orderId, CancellationToken cancellationToken);
    Task<ServiceResult<PublicOrderDto>> ClaimPlayAsync(Guid orderId, ClaimPlayRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<TokenBoardDto>> GetPublicBoardAsync(string restaurantCode, CancellationToken cancellationToken);
}
