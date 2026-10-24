using Billing.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace Billing.Api.Live;

public class FloorHub : Hub
{
    public Task JoinHouse(string restaurantId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, "house:" + restaurantId);
}

public class SignalRHouseLive(IHubContext<FloorHub> hub) : IHouseLive
{
    public Task Changed(Guid restaurantId, CancellationToken cancellationToken = default) =>
        hub.Clients.Group("house:" + restaurantId).SendAsync("changed", restaurantId, cancellationToken);
}
