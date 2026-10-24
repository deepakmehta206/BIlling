namespace Billing.Application.Dtos;

public sealed record BillLineDto(string ItemName, string? VariantName, int Quantity, decimal UnitPrice);

public sealed record OpenTicketDto(
    Guid OrderId,
    int TokenNumber,
    string Status,
    string? TableName,
    string? GuestName,
    decimal Subtotal,
    IReadOnlyList<BillLineDto> Lines,
    string? QrPng,
    decimal Payable);

public sealed record GuestPaymentDto(
    Guid? BillId,
    int? BillNumber,
    decimal Total,
    string Status,
    string QrPng);

public sealed record BillDto(
    Guid Id,
    Guid RestaurantId,
    int BillNumber,
    string? TableName,
    string? GuestName,
    decimal Subtotal,
    decimal GstPercent,
    decimal GstAmount,
    decimal Discount,
    decimal Total,
    string Status,
    string PaymentMode,
    string? UpiUri,
    string? QrPng,
    string? WhatsAppStatus,
    string? WhatsAppUrl,
    IReadOnlyList<BillLineDto> Lines);

public sealed record BillingDeskDto(IReadOnlyList<OpenTicketDto> Tickets, IReadOnlyList<BillDto> Bills);

public sealed class CreateBillRequest
{
    public Guid OrderId { get; set; }
    public string PaymentMode { get; set; } = "Cash";
    public decimal GstPercent { get; set; } = 5;
    public decimal Discount { get; set; }
    public bool Received { get; set; }
    public string? PublicOrigin { get; set; }
}

public sealed record PublicBillDto(
    Guid Id,
    Guid RestaurantId,
    string RestaurantName,
    int BillNumber,
    string? TableName,
    string? GuestName,
    decimal Total,
    string Status,
    string? UpiUri,
    string? QrPng,
    IReadOnlyList<BillLineDto> Lines);
