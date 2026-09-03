using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalLive.Api.Controllers;

[Route("api/chat")]
[Authorize]
public class ChatController : ApiControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var result = await _chatService.GetUserConversationsAsync(RequireUserId());
        return Ok(result);
    }

    [HttpGet("request/{requestId:guid}/shop/{shopId:guid}")]
    public async Task<IActionResult> GetOrCreate(Guid requestId, Guid shopId)
    {
        var result = await _chatService.GetOrCreateConversationAsync(RequireUserId(), requestId, shopId);
        return HandleResult(result);
    }

    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<IActionResult> GetConversation(Guid conversationId)
    {
        var result = await _chatService.GetConversationByIdAsync(RequireUserId(), conversationId);
        return HandleResult(result);
    }

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _chatService.GetMessagesAsync(RequireUserId(), conversationId, page, pageSize);
        return HandleResult(result);
    }

    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid conversationId, [FromBody] SendMessageRequest request)
    {
        var result = await _chatService.SendMessageAsync(RequireUserId(), conversationId, request.Content);
        return HandleResult(result);
    }

    [HttpPost("conversations/{conversationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid conversationId)
    {
        var result = await _chatService.MarkConversationAsReadAsync(RequireUserId(), conversationId);
        return HandleResult(result);
    }
}
