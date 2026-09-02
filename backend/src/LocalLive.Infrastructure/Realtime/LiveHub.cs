using System.Security.Claims;
using LocalLive.Domain.Enums;
using LocalLive.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LocalLive.Infrastructure.Realtime;

public interface ILiveClient
{
    Task NewRequest(object payload);
    Task ShopAvailable(object payload);
    Task RequestStatusChanged(object payload);
    Task RequestClosed(object payload);
}

[Authorize]
public class LiveHub : Hub<ILiveClient>
{
    private readonly AppDbContext _db;

    public LiveHub(AppDbContext db)
    {
        _db = db;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Called by a shop owner's client to join the realtime group for their shop.
    /// Ownership is validated server-side so a shop owner can never join another shop's group.
    /// </summary>
    public async Task JoinShop(Guid shopId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new HubException("Unauthorized");
        }

        var isOwner = await _db.Shops.AnyAsync(s => s.Id == shopId && s.OwnerUserId == userId.Value);
        var isAdmin = GetUserRole() == UserRole.Admin;
        if (!isOwner && !isAdmin)
        {
            throw new HubException("You are not authorized to join this shop's feed.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"shop:{shopId}");
    }

    public async Task LeaveShop(Guid shopId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"shop:{shopId}");
    }

    private Guid? GetUserId()
    {
        var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private UserRole? GetUserRole()
    {
        var role = Context.User?.FindFirstValue(ClaimTypes.Role)
                   ?? Context.User?.FindFirstValue("role");
        return Enum.TryParse<UserRole>(role, true, out var parsed) ? parsed : null;
    }
}
