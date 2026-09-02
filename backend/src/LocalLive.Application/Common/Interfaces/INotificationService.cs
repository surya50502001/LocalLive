using LocalLive.Application.Common;
using LocalLive.Application.Features.Notifications;

namespace LocalLive.Application.Common.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetMyAsync(Guid userId, int take = 50);
    Task<Result> MarkReadAsync(Guid userId, List<Guid>? ids);
    Task<int> GetUnreadCountAsync(Guid userId);
}
