using System.Globalization;
using Billing.Application.Abstractions;
using Billing.Application.Common;
using Billing.Application.Dtos;
using Billing.Domain.Billing;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Services;

public class ReportService(AppDbContext db, HouseAccess access) : IReportService
{
    public async Task<ServiceResult<DayReportDto>> GetAsync(Guid? restaurantId, string? date, CancellationToken cancellationToken)
    {
        var house = await access.RequireReports(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<DayReportDto>.Fail(house.StatusCode, house.Error ?? "Cannot open reports.");

        if (!TryDay(date, out var day))
            return ServiceResult<DayReportDto>.Fail(400, "Use a date like 2026-10-24.");

        var bills = await db.Bills
            .Include(item => item.Lines)
            .Where(item => item.RestaurantId == house.Value.Id && item.ServiceDate == day && item.Status != BillStatuses.Cancelled)
            .OrderByDescending(item => item.BillNumber)
            .ToListAsync(cancellationToken);

        var paid = bills.Where(item => item.Status == BillStatuses.Paid).ToList();
        var unpaid = bills.Where(item => item.Status == BillStatuses.Unpaid).ToList();

        var taxes = paid
            .GroupBy(item => item.GstPercent)
            .OrderBy(group => group.Key)
            .Select(group => new ReportTaxDto(
                group.Key,
                group.Sum(item => item.Subtotal),
                group.Sum(item => item.GstAmount),
                group.Sum(item => item.Total)))
            .ToList();

        var dishes = paid
            .SelectMany(item => item.Lines)
            .GroupBy(line => new { line.ItemName, line.VariantName })
            .Select(group => new ReportDishDto(
                group.Key.ItemName,
                group.Key.VariantName,
                group.Sum(line => line.Quantity),
                group.Sum(line => line.UnitPrice * line.Quantity)))
            .OrderByDescending(item => item.Amount)
            .ThenBy(item => item.ItemName)
            .Take(8)
            .ToList();

        var rows = bills
            .Select(item => new ReportBillDto(
                item.BillNumber,
                item.TableName,
                item.GuestName,
                item.PaymentMode,
                item.Status,
                item.GstPercent,
                item.GstAmount,
                item.Total))
            .ToList();

        return ServiceResult<DayReportDto>.Ok(new DayReportDto(
            day,
            house.Value.Name,
            bills.Count,
            paid.Count,
            unpaid.Count,
            paid.Sum(item => item.Total),
            paid.Where(item => item.PaymentMode == PaymentModes.Cash).Sum(item => item.Total),
            paid.Where(item => item.PaymentMode == PaymentModes.Upi).Sum(item => item.Total),
            unpaid.Sum(item => item.Total),
            paid.Sum(item => item.Discount),
            paid.Sum(item => item.GstAmount),
            taxes,
            dishes,
            rows));
    }

    private static bool TryDay(string? date, out DateOnly day)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            day = IndiaClock.Today();
            return true;
        }

        return DateOnly.TryParseExact(date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day);
    }
}
