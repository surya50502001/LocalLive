using LocalLive.Application.Features.Requests;
using LocalLive.Application.Features.Shops;

namespace LocalLive.Application.Common.Interfaces;

public interface IRealtimeNotifier
{
    /// <summary>Notify a shop owner of a new matching request.</summary>
    Task NotifyShopNewRequestAsync(Guid shopId, object payload);

    /// <summary>Notify a customer that a shop is available.</summary>
    Task NotifyCustomerShopAvailableAsync(Guid customerUserId, object payload);

    /// <summary>Notify a customer or shop that a request status changed.</summary>
    Task NotifyRequestStatusChangedAsync(Guid customerUserId, Guid requestId, string status);

    /// <summary>Notify shop owners that a request was cancelled/expired so they can clear it.</summary>
    Task NotifyShopRequestClosedAsync(Guid requestId);

    /// <summary>Notify participants of a new chat message in a conversation.</summary>
    Task NotifyNewChatMessageAsync(Guid conversationId, object messagePayload);

    /// <summary>Notify participants that a user is typing.</summary>
    Task NotifyUserTypingAsync(Guid conversationId, Guid userId, string userName);

    /// <summary>Notify participants that messages have been read.</summary>
    Task NotifyMessagesReadAsync(Guid conversationId, Guid readByUserId);
}
