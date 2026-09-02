using System.ComponentModel.DataAnnotations;

namespace LocalLive.Application.Features.Notifications;

public record NotificationDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? PayloadJson { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record MarkNotificationsReadRequest
{
    public List<Guid>? Ids { get; init; }
}
