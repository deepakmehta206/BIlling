using System.Globalization;
using Billing.Application.Abstractions;
using Billing.Application.Common;
using Billing.Application.Dtos;
using Billing.Domain.Billing;
using Billing.Domain.Entities;
using Billing.Domain.Floor;
using Billing.Domain.Security;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace Billing.Infrastructure.Services;

public class BillingService(AppDbContext db, HouseAccess access, IRequestContext request, IHouseLive live, IInventoryService stock) : IBillingService
{
    private static readonly decimal[] GstRates = [0, 5, 12, 18];

    public async Task<ServiceResult<BillingDeskDto>> GetDeskAsync(Guid? restaurantId, CancellationToken cancellationToken)
    {
        var house = await access.RequireBilling(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<BillingDeskDto>.Fail(house.StatusCode, house.Error ?? "Cannot open billing.");

        var day = IndiaClock.Today();
        var bills = await db.Bills
            .Include(item => item.Lines)
            .Include(item => item.Notices)
            .Where(item => item.RestaurantId == house.Value.Id && item.ServiceDate == day)
            .OrderByDescending(item => item.BillNumber)
            .ToListAsync(cancellationToken);

        var billed = bills
            .Where(item => item.Status != BillStatuses.Cancelled && item.GuestOrderId is not null)
            .Select(item => item.GuestOrderId!.Value)
            .ToHashSet();

        var orders = await db.GuestOrders
            .Include(item => item.Lines)
            .Where(item => item.RestaurantId == house.Value.Id && item.ServiceDate == day && item.Status != OrderStatuses.Cancelled)
            .OrderBy(item => item.TokenNumber)
            .ToListAsync(cancellationToken);

        var tickets = orders
            .Where(item => !billed.Contains(item.Id))
            .Select(MapTicket)
            .ToList();

        return ServiceResult<BillingDeskDto>.Ok(new BillingDeskDto(tickets, bills.Select(item => MapBill(item, includeQr: false)).ToList()));
    }

    public async Task<ServiceResult<BillDto>> CreateAsync(Guid? restaurantId, CreateBillRequest body, CancellationToken cancellationToken)
    {
        var house = await access.RequireBilling(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<BillDto>.Fail(house.StatusCode, house.Error ?? "Cannot cut a bill.");

        var mode = InputRules.Required(body.PaymentMode);
        if (!PaymentModes.Known(mode))
            return ServiceResult<BillDto>.Fail(400, "Payment is Cash or Upi.");

        if (!GstRates.Contains(body.GstPercent))
            return ServiceResult<BillDto>.Fail(400, "GST is 0, 5, 12 or 18 percent.");

        if (body.Discount < 0)
            return ServiceResult<BillDto>.Fail(400, "Discount cannot be negative.");

        var order = await db.GuestOrders
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == body.OrderId && item.RestaurantId == house.Value.Id, cancellationToken);
        if (order is null || order.Status == OrderStatuses.Cancelled)
            return ServiceResult<BillDto>.Fail(404, "That token is not open.");

        var already = await db.Bills.AnyAsync(
            item => item.GuestOrderId == order.Id && item.Status != BillStatuses.Cancelled,
            cancellationToken);
        if (already)
            return ServiceResult<BillDto>.Fail(409, "This token already has a bill.");

        if (order.Lines.Count == 0)
            return ServiceResult<BillDto>.Fail(400, "This token has no dishes.");

        var subtotal = order.Lines.Sum(line => line.UnitPrice * line.Quantity);
        var gst = decimal.Round(subtotal * body.GstPercent / 100m, 2, MidpointRounding.AwayFromZero);
        if (body.Discount > subtotal + gst)
            return ServiceResult<BillDto>.Fail(400, "Discount is larger than the bill.");

        var total = subtotal + gst - body.Discount;
        var restaurant = await db.Restaurants.FirstAsync(item => item.Id == house.Value.Id, cancellationToken);
        string? upiUri = null;
        if (mode == PaymentModes.Upi)
        {
            if (!await access.IsOn(house.Value.Id, ModuleCodes.Upi, cancellationToken))
                return ServiceResult<BillDto>.Fail(403, "UPI is turned off for the house.");

            if (!InputRules.ValidUpi(restaurant.UpiId) || string.IsNullOrWhiteSpace(restaurant.UpiId))
                return ServiceResult<BillDto>.Fail(400, "Add a UPI id on the house before taking UPI.");
        }

        var day = IndiaClock.Today();
        var number = (await db.Bills
            .Where(item => item.RestaurantId == house.Value.Id && item.ServiceDate == day)
            .MaxAsync(item => (int?)item.BillNumber, cancellationToken) ?? 0) + 1;

        if (mode == PaymentModes.Upi)
            upiUri = BuildUpi(restaurant.UpiId!, restaurant.Name, total, number);

        var bill = new Bill
        {
            Id = Guid.NewGuid(),
            RestaurantId = house.Value.Id,
            GuestOrderId = order.Id,
            BillNumber = number,
            ServiceDate = day,
            TableName = order.TableName,
            GuestName = order.GuestName,
            Subtotal = subtotal,
            GstPercent = body.GstPercent,
            GstAmount = gst,
            Discount = body.Discount,
            Total = total,
            Status = mode == PaymentModes.Cash ? BillStatuses.Paid : BillStatuses.Unpaid,
            PaymentMode = mode,
            UpiUri = upiUri,
        };

        foreach (var line in order.Lines)
        {
            bill.Lines.Add(new BillLine
            {
                Id = Guid.NewGuid(),
                BillId = bill.Id,
                ItemName = line.ItemName,
                VariantName = line.VariantName,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
            });
        }

        order.Status = OrderStatuses.Served;
        if (order.DiningTableId is Guid tableId)
            await ReleaseTable(tableId, order.Id, cancellationToken);

        var notice = await PrepareNotice(restaurant, bill, SafeOrigin(body.PublicOrigin), cancellationToken);
        db.Bills.Add(bill);
        if (notice is not null)
            db.OwnerNotices.Add(notice);
        await stock.ConsumeAsync(house.Value.Id, house.Value.Name, bill, order.Lines.ToList(), cancellationToken);

        db.AuditLogs.Add(AuditEntries.Create(
            request,
            "bill.created",
            "Bill",
            bill.Id.ToString(),
            $"Bill {bill.BillNumber} for {bill.Total:0.00} by {mode}.",
            house.Value.Id,
            house.Value.Name));

        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        if (notice is not null)
            bill.Notices.Add(notice);
        return ServiceResult<BillDto>.Ok(MapBill(bill, includeQr: true));
    }

    public async Task<ServiceResult<BillDto>> MarkPaidAsync(Guid? restaurantId, Guid billId, CancellationToken cancellationToken)
    {
        var house = await access.RequireBilling(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<BillDto>.Fail(house.StatusCode, house.Error ?? "Cannot mark a bill paid.");

        var bill = await db.Bills
            .Include(item => item.Lines)
            .Include(item => item.Notices)
            .FirstOrDefaultAsync(item => item.Id == billId && item.RestaurantId == house.Value.Id, cancellationToken);
        if (bill is null)
            return ServiceResult<BillDto>.Fail(404, "Bill not found.");

        if (bill.Status != BillStatuses.Unpaid)
            return ServiceResult<BillDto>.Fail(409, "Only an unpaid bill can be marked paid.");

        bill.Status = BillStatuses.Paid;
        var restaurant = await db.Restaurants.FirstAsync(item => item.Id == house.Value.Id, cancellationToken);
        var notice = await PrepareNotice(restaurant, bill, payUrl: null, cancellationToken);
        if (notice is not null)
        {
            db.OwnerNotices.Add(notice);
            bill.Notices.Add(notice);
        }

        db.AuditLogs.Add(AuditEntries.Create(
            request,
            "bill.paid",
            "Bill",
            bill.Id.ToString(),
            $"Bill {bill.BillNumber} marked paid.",
            house.Value.Id,
            house.Value.Name));

        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<BillDto>.Ok(MapBill(bill, includeQr: true));
    }

    public async Task<ServiceResult<PublicBillDto>> GetPublicAsync(Guid billId, CancellationToken cancellationToken)
    {
        var bill = await db.Bills.Include(item => item.Lines).FirstOrDefaultAsync(item => item.Id == billId, cancellationToken);
        if (bill is null)
            return ServiceResult<PublicBillDto>.Fail(404, "This bill is not on the book.");

        var restaurant = await db.Restaurants
            .Where(item => item.Id == bill.RestaurantId)
            .Select(item => new { item.Name, item.IsActive })
            .FirstOrDefaultAsync(cancellationToken);
        if (restaurant is null || !restaurant.IsActive)
            return ServiceResult<PublicBillDto>.Fail(404, "This house is closed.");

        var showQr = bill.UpiUri is not null && await access.IsOn(bill.RestaurantId, ModuleCodes.Upi, cancellationToken);
        return ServiceResult<PublicBillDto>.Ok(new PublicBillDto(
            bill.Id,
            bill.RestaurantId,
            restaurant.Name,
            bill.BillNumber,
            bill.TableName,
            bill.GuestName,
            bill.Total,
            bill.Status,
            showQr ? bill.UpiUri : null,
            showQr ? QrPng(bill.UpiUri!) : null,
            bill.Lines.Select(MapLine).ToList()));
    }

    private async Task ReleaseTable(Guid tableId, Guid orderId, CancellationToken cancellationToken)
    {
        var stillOpen = await db.GuestOrders.AnyAsync(
            item => item.Id != orderId && item.DiningTableId == tableId && (item.Status == OrderStatuses.Preparing || item.Status == OrderStatuses.Ready),
            cancellationToken);
        var table = await db.DiningTables.FirstOrDefaultAsync(item => item.Id == tableId, cancellationToken);
        if (!stillOpen && table is not null && table.Status == TableStatuses.Occupied)
            table.Status = TableStatuses.Available;
    }

    private async Task<OwnerNotice?> PrepareNotice(Restaurant restaurant, Bill bill, string? payUrl, CancellationToken cancellationToken)
    {
        if (!await access.IsOn(restaurant.Id, ModuleCodes.WhatsApp, cancellationToken))
            return null;

        var digits = WaDigits(restaurant.OwnerPhone);
        var body = Message(restaurant.Name, bill, payUrl);
        if (digits is null)
        {
            return new OwnerNotice
            {
                Id = Guid.NewGuid(),
                RestaurantId = restaurant.Id,
                BillId = bill.Id,
                Phone = restaurant.OwnerPhone ?? "",
                Body = body,
                Status = NoticeStatuses.Skipped,
            };
        }

        return new OwnerNotice
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurant.Id,
            BillId = bill.Id,
            Phone = digits,
            Body = body,
            Status = NoticeStatuses.Prepared,
            WaUrl = $"https://wa.me/{digits}?text={Uri.EscapeDataString(body)}",
        };
    }

    private static OpenTicketDto MapTicket(GuestOrder order)
    {
        var lines = order.Lines.Select(line => new BillLineDto(line.ItemName, line.VariantName, line.Quantity, line.UnitPrice)).ToList();
        return new OpenTicketDto(order.Id, order.TokenNumber, order.Status, order.TableName, order.GuestName, lines.Sum(line => line.UnitPrice * line.Quantity), lines);
    }

    private static BillDto MapBill(Bill bill, bool includeQr)
    {
        var notice = bill.Notices.OrderByDescending(item => item.CreatedAt).FirstOrDefault();
        return new BillDto(
            bill.Id,
            bill.RestaurantId,
            bill.BillNumber,
            bill.TableName,
            bill.GuestName,
            bill.Subtotal,
            bill.GstPercent,
            bill.GstAmount,
            bill.Discount,
            bill.Total,
            bill.Status,
            bill.PaymentMode,
            bill.UpiUri,
            includeQr && bill.UpiUri is not null ? QrPng(bill.UpiUri) : null,
            notice?.Status,
            notice?.WaUrl,
            bill.Lines.Select(MapLine).ToList());
    }

    private static BillLineDto MapLine(BillLine line) => new(line.ItemName, line.VariantName, line.Quantity, line.UnitPrice);

    private static string Message(string house, Bill bill, string? payUrl)
    {
        var lines = string.Join('\n', bill.Lines.Select(line =>
            $"{line.Quantity} x {line.ItemName}{(string.IsNullOrWhiteSpace(line.VariantName) ? "" : $" ({line.VariantName})")}"));
        var text = $"{house} bill {bill.BillNumber}\n{bill.TableName} · {bill.GuestName}\n{lines}\nTotal ₹{bill.Total.ToString("0.00", CultureInfo.InvariantCulture)} · {bill.Status}";
        if (!string.IsNullOrWhiteSpace(payUrl))
            text += $"\nPay {payUrl}/pay/{bill.Id}";
        return text.Length <= 1000 ? text : text[..1000];
    }

    private static string BuildUpi(string upi, string name, decimal total, int number) =>
        $"upi://pay?pa={Uri.EscapeDataString(upi.Trim())}&pn={Uri.EscapeDataString(name)}&am={total.ToString("0.00", CultureInfo.InvariantCulture)}&cu=INR&tn={Uri.EscapeDataString($"Bill {number}")}";

    private static string? SafeOrigin(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin) || !Uri.TryCreate(origin.Trim(), UriKind.Absolute, out var uri))
            return null;

        return uri.Scheme is "http" or "https" ? uri.GetLeftPart(UriPartial.Authority) : null;
    }

    private static string? WaDigits(string? phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 10)
            return "91" + digits;
        return digits.Length is >= 11 and <= 15 ? digits : null;
    }

    private static string QrPng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(8);
        return Convert.ToBase64String(png);
    }
}
