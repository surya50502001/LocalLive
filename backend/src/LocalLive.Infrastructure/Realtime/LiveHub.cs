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
    Task NewChatMessage(object payload);
    Task UserTyping(object payload);
    Task MessagesRead(object payload);
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

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        var allowed = await _db.Conversations.AnyAsync(c =>
            c.Id == conversationId &&
            (c.CustomerUserId == userId.Value || c.Shop!.OwnerUserId == userId.Value));

        if (allowed || GetUserRole() == UserRole.Admin)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conv:{conversationId}");
        }
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv:{conversationId}");
    }

    public async Task SendTyping(Guid conversationId)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        var name = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "User";
        await Clients.Group($"conv:{conversationId}").UserTyping(new
        {
            conversationId,
            userId = userId.Value,
            userName = name
        });
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
