using Billing.Application.Abstractions;
using Billing.Application.Common;
using Billing.Application.Dtos;
using Billing.Domain.Entities;
using Billing.Domain.Floor;
using Billing.Domain.Security;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Services;

public class FloorService(AppDbContext db, IRequestContext request, HouseAccess access, IHouseLive live, IBillingService billing) : IFloorService
{
    public async Task<ServiceResult<IReadOnlyList<TableDto>>> GetTablesAsync(Guid? restaurantId, DateTime? at, CancellationToken cancellationToken)
    {
        var house = await access.RequireTables(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<IReadOnlyList<TableDto>>.Fail(house.StatusCode, house.Error ?? "Cannot open the floor.");

        var moment = at is DateTime value ? IndiaClock.ToUtc(value) : DateTime.UtcNow;
        var tables = await db.DiningTables
            .Where(item => item.RestaurantId == house.Value.Id)
            .OrderBy(item => item.Section)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var ids = tables.Select(item => item.Id).ToList();
        var bookings = await db.TableBookings
            .Where(item => ids.Contains(item.DiningTableId) && (item.Status == BookingStatuses.Booked || item.Status == BookingStatuses.Seated) && item.StartsAt <= moment && item.EndsAt > moment)
            .ToListAsync(cancellationToken);
        var orders = await db.GuestOrders
            .Where(item => item.DiningTableId != null && ids.Contains(item.DiningTableId.Value) && (item.Status == OrderStatuses.Preparing || item.Status == OrderStatuses.Ready))
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        var rows = tables.Select(table =>
        {
            var booking = bookings.FirstOrDefault(item => item.DiningTableId == table.Id);
            var token = orders.FirstOrDefault(item => item.DiningTableId == table.Id)?.TokenNumber;
            return MapTable(table, booking, token);
        }).ToList();

        return ServiceResult<IReadOnlyList<TableDto>>.Ok(rows);
    }

    public async Task<ServiceResult<TableDto>> SaveTableAsync(Guid? restaurantId, SaveTableRequest body, CancellationToken cancellationToken)
    {
        var house = await access.RequireTables(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<TableDto>.Fail(house.StatusCode, house.Error ?? "Cannot edit tables.");

        var name = InputRules.Required(body.Name);
        var section = InputRules.Required(body.Section);
        if (section.Length == 0)
            section = "Hall";
        if (name.Length is < 1 or > 40)
            return ServiceResult<TableDto>.Fail(400, "Give the table a short name.");
        if (body.Capacity is < 1 or > 30)
            return ServiceResult<TableDto>.Fail(400, "Seats must be between 1 and 30.");
        if (!TableStatuses.Known(body.Status))
            return ServiceResult<TableDto>.Fail(400, "Status must be Available, Occupied or Blocked.");

        var duplicate = await db.DiningTables.AnyAsync(item => item.RestaurantId == house.Value.Id && item.Name == name && item.Id != body.Id, cancellationToken);
        if (duplicate)
            return ServiceResult<TableDto>.Fail(409, "A table with that name is already on the floor.");

        DiningTable table;
        var created = body.Id is null;
        if (body.Id is Guid id)
        {
            table = await db.DiningTables.FirstOrDefaultAsync(item => item.Id == id && item.RestaurantId == house.Value.Id, cancellationToken) ?? null!;
            if (table is null)
                return ServiceResult<TableDto>.Fail(404, "Table not found.");
        }
        else
        {
            var count = await db.DiningTables.CountAsync(item => item.RestaurantId == house.Value.Id, cancellationToken);
            table = new DiningTable
            {
                Id = Guid.NewGuid(),
                RestaurantId = house.Value.Id,
                QrToken = await NewQrToken(cancellationToken),
                PosX = body.PosX == 0 ? 8 + (count % 4) * 22 : Clamp(body.PosX),
                PosY = body.PosY == 0 ? 12 + (count / 4) * 26 : Clamp(body.PosY)
            };
            db.DiningTables.Add(table);
        }

        table.Name = name;
        table.Section = section;
        table.Capacity = body.Capacity;
        table.Status = body.Status;
        if (!created)
        {
            table.PosX = Clamp(body.PosX);
            table.PosY = Clamp(body.PosY);
        }

        db.AuditLogs.Add(AuditEntries.Create(request, created ? "table.created" : "table.updated", "DiningTable", table.Id.ToString(), $"{name} is {table.Status}.", house.Value.Id, house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<TableDto>.Ok(MapTable(table, null, null));
    }

    public async Task<ServiceResult<MessageDto>> DeleteTableAsync(Guid? restaurantId, Guid tableId, CancellationToken cancellationToken)
    {
        var house = await access.RequireTables(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<MessageDto>.Fail(house.StatusCode, house.Error ?? "Cannot edit tables.");

        var table = await db.DiningTables.FirstOrDefaultAsync(item => item.Id == tableId && item.RestaurantId == house.Value.Id, cancellationToken);
        if (table is null)
            return ServiceResult<MessageDto>.Fail(404, "Table not found.");

        table.IsDeleted = true;
        db.AuditLogs.Add(AuditEntries.Create(request, "table.deleted", "DiningTable", table.Id.ToString(), $"Archived table {table.Name}.", house.Value.Id, house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<MessageDto>.Ok(new MessageDto($"{table.Name} was archived."));
    }

    public async Task<ServiceResult<IReadOnlyList<BookingDto>>> GetBookingsAsync(Guid? restaurantId, DateOnly? day, CancellationToken cancellationToken)
    {
        var house = await access.RequireBooking(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<IReadOnlyList<BookingDto>>.Fail(house.StatusCode, house.Error ?? "Cannot open bookings.");

        var range = IndiaClock.DayRange(day ?? IndiaClock.Today());
        var rows = await db.TableBookings
            .Where(item => item.RestaurantId == house.Value.Id && item.StartsAt >= range.Start && item.StartsAt < range.End)
            .OrderBy(item => item.StartsAt)
            .Select(item => new BookingDto(item.Id, item.DiningTableId, item.TableName, item.GuestName, item.GuestPhone, item.GuestCount, item.StartsAt, item.EndsAt, item.Status, item.Notes))
            .ToListAsync(cancellationToken);
        return ServiceResult<IReadOnlyList<BookingDto>>.Ok(rows);
    }

    public async Task<ServiceResult<BookingDto>> CreateBookingAsync(Guid? restaurantId, CreateBookingRequest body, CancellationToken cancellationToken)
    {
        var house = await access.RequireBooking(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<BookingDto>.Fail(house.StatusCode, house.Error ?? "Cannot book a table.");

        var guest = InputRules.Required(body.GuestName);
        var phone = InputRules.Required(body.GuestPhone);
        if (guest.Length < 2)
            return ServiceResult<BookingDto>.Fail(400, "Enter the guest's name.");
        if (!InputRules.ValidPhone(phone))
            return ServiceResult<BookingDto>.Fail(400, "Guest phone needs 10 to 15 digits.");
        if (body.DurationMinutes is < 30 or > 240)
            return ServiceResult<BookingDto>.Fail(400, "A booking runs from 30 minutes to 4 hours.");

        var table = await db.DiningTables.FirstOrDefaultAsync(item => item.Id == body.TableId && item.RestaurantId == house.Value.Id, cancellationToken);
        if (table is null)
            return ServiceResult<BookingDto>.Fail(404, "Table not found.");
        if (table.Status == TableStatuses.Blocked)
            return ServiceResult<BookingDto>.Fail(409, "That table is blocked.");
        if (body.GuestCount is < 1 || body.GuestCount > table.Capacity)
            return ServiceResult<BookingDto>.Fail(400, $"{table.Name} seats {table.Capacity}.");

        var starts = IndiaClock.ToUtc(body.StartsAt);
        var ends = starts.AddMinutes(body.DurationMinutes);
        var clash = await db.TableBookings.AnyAsync(
            item => item.DiningTableId == table.Id && (item.Status == BookingStatuses.Booked || item.Status == BookingStatuses.Seated) && item.StartsAt < ends && item.EndsAt > starts,
            cancellationToken);
        if (clash)
            return ServiceResult<BookingDto>.Fail(409, "That table is already booked for this time.");

        var booking = new TableBooking
        {
            Id = Guid.NewGuid(),
            RestaurantId = house.Value.Id,
            DiningTableId = table.Id,
            TableName = table.Name,
            GuestName = guest,
            GuestPhone = phone,
            GuestCount = body.GuestCount,
            StartsAt = starts,
            EndsAt = ends,
            Status = BookingStatuses.Booked,
            Notes = InputRules.Clean(body.Notes)
        };
        db.TableBookings.Add(booking);
        db.AuditLogs.Add(AuditEntries.Create(request, "booking.created", "TableBooking", booking.Id.ToString(), $"{guest} booked {table.Name}.", house.Value.Id, house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<BookingDto>.Ok(new BookingDto(booking.Id, booking.DiningTableId, booking.TableName, booking.GuestName, booking.GuestPhone, booking.GuestCount, booking.StartsAt, booking.EndsAt, booking.Status, booking.Notes));
    }

    public async Task<ServiceResult<BookingDto>> CancelBookingAsync(Guid? restaurantId, Guid bookingId, CancellationToken cancellationToken)
    {
        var house = await access.RequireBooking(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<BookingDto>.Fail(house.StatusCode, house.Error ?? "Cannot change bookings.");

        var booking = await db.TableBookings.FirstOrDefaultAsync(item => item.Id == bookingId && item.RestaurantId == house.Value.Id, cancellationToken);
        if (booking is null)
            return ServiceResult<BookingDto>.Fail(404, "Booking not found.");

        booking.Status = BookingStatuses.Cancelled;
        db.AuditLogs.Add(AuditEntries.Create(request, "booking.cancelled", "TableBooking", booking.Id.ToString(), $"Cancelled {booking.GuestName} on {booking.TableName}.", house.Value.Id, house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<BookingDto>.Ok(new BookingDto(booking.Id, booking.DiningTableId, booking.TableName, booking.GuestName, booking.GuestPhone, booking.GuestCount, booking.StartsAt, booking.EndsAt, booking.Status, booking.Notes));
    }

    public async Task<ServiceResult<IReadOnlyList<TicketDto>>> GetTicketsAsync(Guid? restaurantId, CancellationToken cancellationToken)
    {
        var house = await access.RequireTickets(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<IReadOnlyList<TicketDto>>.Fail(house.StatusCode, house.Error ?? "Cannot open tickets.");

        var day = IndiaClock.Today();
        var rows = await OpenTickets(house.Value.Id, day, cancellationToken);
        return ServiceResult<IReadOnlyList<TicketDto>>.Ok(rows);
    }

    public async Task<ServiceResult<TicketDto>> SetOrderStatusAsync(Guid? restaurantId, Guid orderId, SetOrderStatusRequest body, CancellationToken cancellationToken)
    {
        var house = await access.RequireTickets(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<TicketDto>.Fail(house.StatusCode, house.Error ?? "Cannot update a ticket.");

        var status = InputRules.Required(body.Status);
        if (!OrderStatuses.Known(status))
            return ServiceResult<TicketDto>.Fail(400, "Status must be Preparing, Ready, Served or Cancelled.");

        var order = await db.GuestOrders.Include(item => item.Lines).FirstOrDefaultAsync(item => item.Id == orderId && item.RestaurantId == house.Value.Id, cancellationToken);
        if (order is null)
            return ServiceResult<TicketDto>.Fail(404, "Order not found.");

        order.Status = status;
        if (order.DiningTableId is Guid tableId && !OrderStatuses.Open(status))
        {
            var stillOpen = await db.GuestOrders.AnyAsync(
                item => item.Id != order.Id && item.DiningTableId == tableId && (item.Status == OrderStatuses.Preparing || item.Status == OrderStatuses.Ready),
                cancellationToken);
            var table = await db.DiningTables.FirstOrDefaultAsync(item => item.Id == tableId, cancellationToken);
            if (!stillOpen && table is not null && table.Status == TableStatuses.Occupied)
                table.Status = TableStatuses.Available;
        }

        db.AuditLogs.Add(AuditEntries.Create(request, "order.status", "GuestOrder", order.Id.ToString(), $"Token {order.TokenNumber} is {status}.", house.Value.Id, house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<TicketDto>.Ok(MapTicket(order));
    }

    public async Task<ServiceResult<PublicTableDto>> GetPublicTableAsync(string qrToken, CancellationToken cancellationToken)
    {
        var table = await FindTable(qrToken, cancellationToken);
        if (table is null)
            return ServiceResult<PublicTableDto>.Fail(404, "This table code is not on the floor.");

        var house = await access.RequirePublic(table.RestaurantId, ModuleCodes.QrOrder, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<PublicTableDto>.Fail(house.StatusCode, house.Error ?? "Ordering is off.");

        var restaurant = await db.Restaurants.Where(item => item.Id == table.RestaurantId).Select(item => new { item.Name, item.Code }).FirstAsync(cancellationToken);
        var items = await db.MenuItems
            .Where(item => item.RestaurantId == table.RestaurantId && item.IsAvailable && item.Category.IsActive)
            .OrderBy(item => item.Category.SortOrder)
            .ThenBy(item => item.SortOrder)
            .Select(item => new
            {
                item.Category.Name,
                item.Id,
                item.Description,
                item.FoodType,
                item.Price,
                item.ImagePath,
                Stamp = item.UpdatedAt ?? item.CreatedAt,
                DishName = item.Name,
                Variants = item.Variants.OrderBy(variant => variant.Price).Select(variant => new NamedPrice(variant.Id, variant.Name, variant.Price)).ToList(),
                Addons = item.Addons.OrderBy(addon => addon.Name).Select(addon => new NamedPrice(addon.Id, addon.Name, addon.Price)).ToList()
            })
            .ToListAsync(cancellationToken);

        var menu = items
            .GroupBy(item => item.Name)
            .Select(group => new PublicCategoryDto(group.Key, group.Select(item => new PublicDishDto(
                item.Id,
                item.DishName,
                item.Description,
                item.FoodType,
                item.Price,
                item.ImagePath is null ? null : $"{item.ImagePath}?v={new DateTimeOffset(DateTime.SpecifyKind(item.Stamp, DateTimeKind.Utc)).ToUnixTimeSeconds()}",
                item.Variants,
                item.Addons)).ToList()))
            .ToList();

        return ServiceResult<PublicTableDto>.Ok(new PublicTableDto(table.RestaurantId, restaurant.Name, restaurant.Code, table.Name, menu));
    }

    public async Task<ServiceResult<PublicOrderDto>> PlacePublicOrderAsync(string qrToken, PlaceOrderRequest body, CancellationToken cancellationToken)
    {
        var table = await FindTable(qrToken, cancellationToken);
        if (table is null)
            return ServiceResult<PublicOrderDto>.Fail(404, "This table code is not on the floor.");
        if (table.Status == TableStatuses.Blocked)
            return ServiceResult<PublicOrderDto>.Fail(409, "This table is not in service.");

        var house = await access.RequirePublic(table.RestaurantId, ModuleCodes.QrOrder, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<PublicOrderDto>.Fail(house.StatusCode, house.Error ?? "Ordering is off.");

        var requested = body.Lines ?? [];
        if (requested.Count is 0 or > 30)
            return ServiceResult<PublicOrderDto>.Fail(400, "Add at least one dish.");

        var itemIds = requested.Select(line => line.ItemId).Distinct().ToList();
        var dishes = await db.MenuItems
            .Include(item => item.Variants)
            .Include(item => item.Addons)
            .Where(item => item.RestaurantId == table.RestaurantId && itemIds.Contains(item.Id) && item.IsAvailable)
            .ToListAsync(cancellationToken);

        var lines = new List<OrderLine>();
        foreach (var line in requested)
        {
            if (line.Quantity is < 1 or > 20)
                return ServiceResult<PublicOrderDto>.Fail(400, "Quantity must be between 1 and 20.");

            var dish = dishes.FirstOrDefault(item => item.Id == line.ItemId);
            if (dish is null)
                return ServiceResult<PublicOrderDto>.Fail(400, "A dish is no longer available.");

            ItemVariant? variant = null;
            if (dish.Variants.Count > 0)
            {
                variant = dish.Variants.FirstOrDefault(item => item.Id == line.VariantId);
                if (variant is null)
                    return ServiceResult<PublicOrderDto>.Fail(400, $"Choose a size for {dish.Name}.");
            }

            var addons = dish.Addons.Where(item => (line.AddonIds ?? []).Contains(item.Id)).ToList();
            var unit = (variant?.Price ?? dish.Price) + addons.Sum(item => item.Price);
            lines.Add(new OrderLine
            {
                Id = Guid.NewGuid(),
                ItemName = dish.Name,
                MenuItemId = dish.Id,
                VariantName = variant?.Name,
                AddonSummary = addons.Count == 0 ? null : string.Join(", ", addons.Select(item => item.Name)),
                Quantity = line.Quantity,
                UnitPrice = unit,
                Note = InputRules.Clean(line.Note)
            });
        }

        var day = IndiaClock.Today();
        var token = (await db.GuestOrders.Where(item => item.RestaurantId == table.RestaurantId && item.ServiceDate == day).MaxAsync(item => (int?)item.TokenNumber, cancellationToken) ?? 0) + 1;
        var order = new GuestOrder
        {
            Id = Guid.NewGuid(),
            RestaurantId = table.RestaurantId,
            DiningTableId = table.Id,
            TableName = table.Name,
            TokenNumber = token,
            ServiceDate = day,
            Status = OrderStatuses.Preparing,
            GuestName = InputRules.Clean(body.GuestName),
            Note = InputRules.Clean(body.Note)
        };
        foreach (var line in lines)
            line.GuestOrderId = order.Id;

        if (table.Status != TableStatuses.Blocked)
            table.Status = TableStatuses.Occupied;

        db.GuestOrders.Add(order);
        db.OrderLines.AddRange(lines);
        db.AuditLogs.Add(AuditEntries.Create(request, "order.placed", "GuestOrder", order.Id.ToString(), $"Token {token} from {table.Name}.", table.RestaurantId, house.Value.Name, order.GuestName ?? "guest"));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(table.RestaurantId, cancellationToken);
        order.Lines = lines;
        var payment = await billing.PaymentForAsync(order, cancellationToken);
        return ServiceResult<PublicOrderDto>.Ok(MapPublic(order, house.Value.Name, payment));
    }

    public async Task<ServiceResult<PublicOrderDto>> GetPublicOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await db.GuestOrders.Include(item => item.Lines).FirstOrDefaultAsync(item => item.Id == orderId, cancellationToken);
        if (order is null)
            return ServiceResult<PublicOrderDto>.Fail(404, "Order not found.");

        var name = await db.Restaurants.Where(item => item.Id == order.RestaurantId).Select(item => item.Name).FirstAsync(cancellationToken);
        var payment = await billing.PaymentForAsync(order, cancellationToken);
        return ServiceResult<PublicOrderDto>.Ok(MapPublic(order, name, payment));
    }

    public async Task<ServiceResult<TokenBoardDto>> GetPublicBoardAsync(string restaurantCode, CancellationToken cancellationToken)
    {
        var code = InputRules.Required(restaurantCode).ToUpperInvariant();
        var restaurant = await db.Restaurants.Where(item => item.Code == code).Select(item => new { item.Id, item.Name, item.Code, item.IsActive }).FirstOrDefaultAsync(cancellationToken);
        if (restaurant is null || !restaurant.IsActive)
            return ServiceResult<TokenBoardDto>.Fail(404, "Restaurant not found.");

        var house = await access.RequirePublic(restaurant.Id, ModuleCodes.TokenDisplay, cancellationToken);
        if (!house.Succeeded)
            return ServiceResult<TokenBoardDto>.Fail(house.StatusCode, house.Error ?? "The token screen is off.");

        var tickets = await OpenTickets(restaurant.Id, IndiaClock.Today(), cancellationToken);
        return ServiceResult<TokenBoardDto>.Ok(new TokenBoardDto(restaurant.Id, restaurant.Name, restaurant.Code, tickets));
    }

    private async Task<List<TicketDto>> OpenTickets(Guid restaurantId, DateOnly day, CancellationToken cancellationToken)
    {
        var orders = await db.GuestOrders
            .Include(item => item.Lines)
            .Where(item => item.RestaurantId == restaurantId && item.ServiceDate == day && (item.Status == OrderStatuses.Preparing || item.Status == OrderStatuses.Ready))
            .OrderBy(item => item.TokenNumber)
            .ToListAsync(cancellationToken);
        return orders.Select(MapTicket).ToList();
    }

    private async Task<DiningTable?> FindTable(string qrToken, CancellationToken cancellationToken)
    {
        var token = InputRules.Required(qrToken).ToLowerInvariant();
        return await db.DiningTables.FirstOrDefaultAsync(item => item.QrToken == token, cancellationToken);
    }

    private async Task<string> NewQrToken(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var token = Guid.NewGuid().ToString("N")[..10];
            if (!await db.DiningTables.AnyAsync(item => item.QrToken == token, cancellationToken))
                return token;
        }

        return Guid.NewGuid().ToString("N")[..10];
    }

    private static decimal Clamp(decimal value) => value < 0 ? 0 : value > 92 ? 92 : value;

    private static TableDto MapTable(DiningTable table, TableBooking? booking, int? token) =>
        new(
            table.Id,
            table.Name,
            table.Section,
            table.Capacity,
            table.PosX,
            table.PosY,
            table.Status,
            table.QrToken,
            table.CreatedIp,
            booking is null ? null : new TableBookingHint(booking.Id, booking.GuestName, booking.StartsAt, booking.EndsAt),
            token);

    private static TicketDto MapTicket(GuestOrder order) =>
        new(
            order.Id,
            order.TokenNumber,
            order.Status,
            order.TableName,
            order.GuestName,
            order.CreatedAt,
            order.Lines.Select(line => new TicketLineDto(line.ItemName, line.VariantName, line.AddonSummary, line.Quantity, line.UnitPrice)).ToList());

    private static PublicOrderDto MapPublic(GuestOrder order, string restaurantName, GuestPaymentDto? payment) =>
        new(
            order.Id,
            order.RestaurantId,
            restaurantName,
            order.TableName,
            order.TokenNumber,
            order.Status,
            order.Lines.Select(line => new TicketLineDto(line.ItemName, line.VariantName, line.AddonSummary, line.Quantity, line.UnitPrice)).ToList(),
            payment?.BillId,
            payment?.BillNumber,
            payment?.Total,
            payment?.Status,
            payment?.QrPng);
}

public static class IndiaClock
{
    private static readonly TimeZoneInfo Zone = Load();

    public static DateOnly Today() => DateOnly.FromDateTime(LocalNow());

    public static DateTime LocalNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    public static bool ArrivedLate(TimeOnly startsAt, int graceMinutes, DateTime checkInUtc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(checkInUtc, DateTimeKind.Utc), Zone);
        var due = startsAt.Add(TimeSpan.FromMinutes(Math.Max(graceMinutes, 0)));
        return TimeOnly.FromDateTime(local) > due;
    }

    public static DateTime ToUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), Zone);

    public static (DateTime Start, DateTime End) DayRange(DateOnly day)
    {
        var start = ToUtc(day.ToDateTime(TimeOnly.MinValue));
        return (start, start.AddDays(1));
    }

    private static TimeZoneInfo Load()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
    }
}
