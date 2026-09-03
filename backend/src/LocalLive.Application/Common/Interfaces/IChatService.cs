using LocalLive.Application.Common;
using LocalLive.Application.Features.Chat;

namespace LocalLive.Application.Common.Interfaces;

public interface IChatService
{
    Task<Result<ConversationDto>> GetOrCreateConversationAsync(Guid userId, Guid requestId, Guid shopId);
    Task<Result<ConversationDto>> GetConversationByIdAsync(Guid userId, Guid conversationId);
    Task<List<ConversationDto>> GetUserConversationsAsync(Guid userId);
    Task<Result<List<ChatMessageDto>>> GetMessagesAsync(Guid userId, Guid conversationId, int page = 1, int pageSize = 50);
    Task<Result<ChatMessageDto>> SendMessageAsync(Guid senderUserId, Guid conversationId, string content);
    Task<Result> MarkConversationAsReadAsync(Guid userId, Guid conversationId);
}
