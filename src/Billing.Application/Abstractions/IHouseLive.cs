namespace Billing.Application.Abstractions;

public interface IHouseLive
{
    Task Changed(Guid restaurantId, CancellationToken cancellationToken = default);
}
