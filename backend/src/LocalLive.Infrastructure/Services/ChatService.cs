using LocalLive.Application.Common;
using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Chat;
using LocalLive.Domain.Entities;
using LocalLive.Domain.Enums;
using LocalLive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocalLive.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly AppDbContext _db;
    private readonly IRealtimeNotifier _realtime;
    private readonly ILogger<ChatService> _logger;

    public ChatService(AppDbContext db, IRealtimeNotifier realtime, ILogger<ChatService> logger)
    {
        _db = db;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task<Result<ConversationDto>> GetOrCreateConversationAsync(Guid userId, Guid requestId, Guid shopId)
    {
        var request = await _db.LiveRequests.FirstOrDefaultAsync(r => r.Id == requestId);
        if (request is null)
        {
            return Result<ConversationDto>.Failure(new Error(ErrorType.NotFound, "REQUEST_NOT_FOUND", "Request not found."));
        }

        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Id == shopId);
        if (shop is null)
        {
            return Result<ConversationDto>.Failure(new Error(ErrorType.NotFound, "SHOP_NOT_FOUND", "Shop not found."));
        }

        var isCustomer = request.CustomerUserId == userId;
        var isShopOwner = shop.OwnerUserId == userId;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var isAdmin = user?.Role == UserRole.Admin;

        if (!isCustomer && !isShopOwner && !isAdmin)
        {
            return Result<ConversationDto>.Failure(new Error(ErrorType.Forbidden, "FORBIDDEN", "You are not a participant in this request."));
        }

        var conv = await _db.Conversations
            .Include(c => c.Request)
            .Include(c => c.Shop)
            .Include(c => c.CustomerUser)
            .FirstOrDefaultAsync(c => c.RequestId == requestId && c.ShopId == shopId);

        if (conv is null)
        {
            conv = new Conversation
            {
                RequestId = requestId,
                ShopId = shopId,
                CustomerUserId = request.CustomerUserId,
                LastMessageAt = DateTime.UtcNow
            };
            _db.Conversations.Add(conv);
            await _db.SaveChangesAsync();

            // Reload with navigations
            conv = await _db.Conversations
                .Include(c => c.Request)
                .Include(c => c.Shop)
                .Include(c => c.CustomerUser)
                .FirstAsync(c => c.Id == conv.Id);
        }

        return Result<ConversationDto>.Success(await MapToDtoAsync(conv, userId));
    }

    public async Task<Result<ConversationDto>> GetConversationByIdAsync(Guid userId, Guid conversationId)
    {
        var conv = await _db.Conversations
            .Include(c => c.Request)
            .Include(c => c.Shop)
            .Include(c => c.CustomerUser)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conv is null)
        {
            return Result<ConversationDto>.Failure(new Error(ErrorType.NotFound, "CONVERSATION_NOT_FOUND", "Conversation not found."));
        }

        var isParticipant = conv.CustomerUserId == userId || conv.Shop?.OwnerUserId == userId;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (!isParticipant && user?.Role != UserRole.Admin)
        {
            return Result<ConversationDto>.Failure(new Error(ErrorType.Forbidden, "FORBIDDEN", "You do not have access to this conversation."));
        }

        return Result<ConversationDto>.Success(await MapToDtoAsync(conv, userId));
    }

    public async Task<List<ConversationDto>> GetUserConversationsAsync(Guid userId)
    {
        var convs = await _db.Conversations
            .Include(c => c.Request)
            .Include(c => c.Shop)
            .Include(c => c.CustomerUser)
            .Where(c => c.CustomerUserId == userId || c.Shop!.OwnerUserId == userId)
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync();

        var result = new List<ConversationDto>();
        foreach (var c in convs)
        {
            result.Add(await MapToDtoAsync(c, userId));
        }
        return result;
    }

    public async Task<Result<List<ChatMessageDto>>> GetMessagesAsync(Guid userId, Guid conversationId, int page = 1, int pageSize = 50)
    {
        var conv = await _db.Conversations
            .Include(c => c.Shop)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conv is null)
        {
            return Result<List<ChatMessageDto>>.Failure(new Error(ErrorType.NotFound, "CONVERSATION_NOT_FOUND", "Conversation not found."));
        }

        var isParticipant = conv.CustomerUserId == userId || conv.Shop?.OwnerUserId == userId;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (!isParticipant && user?.Role != UserRole.Admin)
        {
            return Result<List<ChatMessageDto>>.Failure(new Error(ErrorType.Forbidden, "FORBIDDEN", "Access denied."));
        }

        var messages = await _db.ChatMessages
            .Include(m => m.SenderUser)
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderUserId = m.SenderUserId,
                SenderName = m.SenderUser != null ? m.SenderUser.FullName : "User",
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                IsRead = m.IsRead
            })
            .ToListAsync();

        return Result<List<ChatMessageDto>>.Success(messages);
    }

    public async Task<Result<ChatMessageDto>> SendMessageAsync(Guid senderUserId, Guid conversationId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Result<ChatMessageDto>.Failure(new Error(ErrorType.Validation, "EMPTY_MESSAGE", "Message content cannot be empty."));
        }

        var conv = await _db.Conversations
            .Include(c => c.Shop)
            .Include(c => c.CustomerUser)
            .Include(c => c.Request)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conv is null)
        {
            return Result<ChatMessageDto>.Failure(new Error(ErrorType.NotFound, "CONVERSATION_NOT_FOUND", "Conversation not found."));
        }

        var isCustomer = conv.CustomerUserId == senderUserId;
        var isShopOwner = conv.Shop?.OwnerUserId == senderUserId;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == senderUserId);
        var isAdmin = user?.Role == UserRole.Admin;

        if (!isCustomer && !isShopOwner && !isAdmin)
        {
            return Result<ChatMessageDto>.Failure(new Error(ErrorType.Forbidden, "FORBIDDEN", "You cannot post in this conversation."));
        }

        var msg = new ChatMessage
        {
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _db.ChatMessages.Add(msg);
        conv.LastMessageAt = msg.CreatedAt;

        // Recipient for in-app notification
        var recipientUserId = isCustomer ? conv.Shop?.OwnerUserId : conv.CustomerUserId;
        if (recipientUserId.HasValue && recipientUserId.Value != Guid.Empty)
        {
            _db.Notifications.Add(new Notification
            {
                RecipientUserId = recipientUserId.Value,
                Type = NotificationType.System,
                Title = $"New message from {user?.FullName ?? "User"}",
                Body = content.Length > 80 ? $"{content[..80]}..." : content,
                LinkedEntity = "Conversation",
                LinkedEntityId = conversationId,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { conversationId, requestId = conv.RequestId })
            });
        }

        await _db.SaveChangesAsync();

        var dto = new ChatMessageDto
        {
            Id = msg.Id,
            ConversationId = msg.ConversationId,
            SenderUserId = msg.SenderUserId,
            SenderName = user?.FullName ?? "User",
            Content = msg.Content,
            CreatedAt = msg.CreatedAt,
            IsRead = false
        };

        await _realtime.NotifyNewChatMessageAsync(conversationId, dto);

        return Result<ChatMessageDto>.Success(dto);
    }

    public async Task<Result> MarkConversationAsReadAsync(Guid userId, Guid conversationId)
    {
        var unread = await _db.ChatMessages
            .Where(m => m.ConversationId == conversationId && m.SenderUserId != userId && !m.IsRead)
            .ToListAsync();

        if (unread.Count > 0)
        {
            var now = DateTime.UtcNow;
            foreach (var m in unread)
            {
                m.IsRead = true;
                m.ReadAt = now;
            }
            await _db.SaveChangesAsync();
            await _realtime.NotifyMessagesReadAsync(conversationId, userId);
        }

        return Result.Success();
    }

    private async Task<ConversationDto> MapToDtoAsync(Conversation c, Guid currentUserId)
    {
        var lastMsg = await _db.ChatMessages
            .Where(m => m.ConversationId == c.Id)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        var unread = await _db.ChatMessages
            .CountAsync(m => m.ConversationId == c.Id && m.SenderUserId != currentUserId && !m.IsRead);

        return new ConversationDto
        {
            Id = c.Id,
            RequestId = c.RequestId,
            RequestTitle = c.Request?.Title ?? "LIVE Request",
            CustomerUserId = c.CustomerUserId,
            CustomerName = c.CustomerUser?.FullName ?? "Customer",
            ShopId = c.ShopId,
            ShopName = c.Shop?.Name ?? "Shop",
            LastMessageAt = c.LastMessageAt ?? c.CreatedAt,
            LastMessageContent = lastMsg?.Content,
            UnreadCount = unread,
            IsClosed = c.IsClosed
        };
    }
}
