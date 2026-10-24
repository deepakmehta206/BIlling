using Billing.Application.Common;
using Billing.Application.Dtos;
using Billing.Domain.Entities;

namespace Billing.Application.Abstractions;

public interface IBillingService
{
    Task<ServiceResult<BillingDeskDto>> GetDeskAsync(Guid? restaurantId, CancellationToken cancellationToken);
    Task<ServiceResult<BillDto>> CreateAsync(Guid? restaurantId, CreateBillRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<BillDto>> MarkPaidAsync(Guid? restaurantId, Guid billId, CancellationToken cancellationToken);
    Task<ServiceResult<PublicBillDto>> GetPublicAsync(Guid billId, CancellationToken cancellationToken);
    Task<GuestPaymentDto?> PaymentForAsync(GuestOrder order, CancellationToken cancellationToken);
}

public interface IReportService
{
    Task<ServiceResult<DayReportDto>> GetAsync(Guid? restaurantId, string? date, CancellationToken cancellationToken);
}
