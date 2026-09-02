using LocalLive.Application.Common;
using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Notifications;
using LocalLive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocalLive.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<NotificationDto>> GetMyAsync(Guid userId, int take = 50)
        => await _db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Title = n.Title,
                Body = n.Body,
                PayloadJson = n.PayloadJson,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

    public async Task<Result> MarkReadAsync(Guid userId, List<Guid>? ids)
    {
        var query = _db.Notifications.Where(n => n.RecipientUserId == userId && !n.IsRead);
        if (ids is { Count: > 0 })
        {
            query = query.Where(n => ids.Contains(n.Id));
        }

        var items = await query.ToListAsync();
        foreach (var n in items)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
        => await _db.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead);
}
