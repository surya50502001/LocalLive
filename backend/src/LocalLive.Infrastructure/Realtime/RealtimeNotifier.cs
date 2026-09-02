using LocalLive.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LocalLive.Infrastructure.Realtime;

public class RealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<LiveHub, ILiveClient> _hub;

    public RealtimeNotifier(IHubContext<LiveHub, ILiveClient> hub)
    {
        _hub = hub;
    }

    public Task NotifyShopNewRequestAsync(Guid shopId, object payload)
        => _hub.Clients.Group($"shop:{shopId}").NewRequest(payload);

    public Task NotifyCustomerShopAvailableAsync(Guid customerUserId, object payload)
        => _hub.Clients.Group($"user:{customerUserId}").ShopAvailable(payload);

    public Task NotifyRequestStatusChangedAsync(Guid customerUserId, Guid requestId, string status)
        => _hub.Clients.Group($"user:{customerUserId}")
            .RequestStatusChanged(new { requestId, status });

    public Task NotifyShopRequestClosedAsync(Guid requestId)
        => _hub.Clients.All.RequestClosed(new { requestId, closed = true });
}
