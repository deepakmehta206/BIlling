namespace Billing.Application.Dtos;

public sealed record NamedPrice(Guid? Id, string Name, decimal Price);

public sealed record MenuCategoryDto(Guid Id, string Name, int SortOrder, bool IsActive, int ItemCount);

public sealed record MenuItemDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    string FoodType,
    decimal Price,
    bool IsAvailable,
    int SortOrder,
    string? ImageUrl,
    IReadOnlyList<NamedPrice> Variants,
    IReadOnlyList<NamedPrice> Addons);

public sealed record MenuBoardDto(IReadOnlyList<MenuCategoryDto> Categories, IReadOnlyList<MenuItemDto> Items);

public sealed class SaveCategoryRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SaveItemRequest
{
    public Guid? Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FoodType { get; set; } = "Veg";
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int SortOrder { get; set; }
    public string? ImageData { get; set; }
    public bool RemoveImage { get; set; }
    public List<NamedPrice> Variants { get; set; } = [];
    public List<NamedPrice> Addons { get; set; } = [];
}

public sealed record TableBookingHint(Guid Id, string GuestName, DateTime StartsAt, DateTime EndsAt);

public sealed record TableDto(
    Guid Id,
    string Name,
    string Section,
    int Capacity,
    decimal PosX,
    decimal PosY,
    string Status,
    string QrToken,
    string? CreatedIp,
    TableBookingHint? Booking,
    int? OpenToken);

public sealed class SaveTableRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Section { get; set; } = "Hall";
    public int Capacity { get; set; } = 4;
    public decimal PosX { get; set; }
    public decimal PosY { get; set; }
    public string Status { get; set; } = "Available";
}

public sealed record BookingDto(
    Guid Id,
    Guid TableId,
    string TableName,
    string GuestName,
    string GuestPhone,
    int GuestCount,
    DateTime StartsAt,
    DateTime EndsAt,
    string Status,
    string? Notes);

public sealed class CreateBookingRequest
{
    public Guid TableId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public int GuestCount { get; set; } = 2;
    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; } = 90;
    public string? Notes { get; set; }
}

public sealed record TicketLineDto(string ItemName, string? VariantName, string? AddonSummary, int Quantity, decimal UnitPrice);

public sealed record TicketDto(
    Guid Id,
    int TokenNumber,
    string Status,
    string? TableName,
    string? GuestName,
    DateTime CreatedAt,
    IReadOnlyList<TicketLineDto> Lines,
    int? PlayDiscountPercent);

public sealed class SetOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public sealed record PublicDishDto(
    Guid Id,
    string Name,
    string? Description,
    string FoodType,
    decimal Price,
    string? ImageUrl,
    IReadOnlyList<NamedPrice> Variants,
    IReadOnlyList<NamedPrice> Addons);

public sealed record PublicCategoryDto(string Name, IReadOnlyList<PublicDishDto> Items);

public sealed record PublicTableDto(
    Guid RestaurantId,
    string RestaurantName,
    string RestaurantCode,
    string TableName,
    IReadOnlyList<PublicCategoryDto> Menu);

public sealed class PlaceOrderLine
{
    public Guid ItemId { get; set; }
    public Guid? VariantId { get; set; }
    public List<Guid> AddonIds { get; set; } = [];
    public int Quantity { get; set; } = 1;
    public string? Note { get; set; }
}

public sealed class PlaceOrderRequest
{
    public string? GuestName { get; set; }
    public string? Note { get; set; }
    public List<PlaceOrderLine> Lines { get; set; } = [];
}

public sealed class ClaimPlayRequest
{
    public int Score { get; set; }
}

public sealed record PublicOrderDto(
    Guid Id,
    Guid RestaurantId,
    string RestaurantName,
    string? TableName,
    int TokenNumber,
    string Status,
    IReadOnlyList<TicketLineDto> Lines,
    Guid? BillId,
    int? BillNumber,
    decimal? Payable,
    string? PaymentStatus,
    string? QrPng,
    int? PlayDiscountPercent,
    bool CanPlay);

public sealed record TokenBoardDto(Guid RestaurantId, string RestaurantName, string Code, IReadOnlyList<TicketDto> Tickets);
