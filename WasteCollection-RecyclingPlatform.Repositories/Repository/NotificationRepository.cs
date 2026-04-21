using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Data;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;

    public NotificationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Notification>> GetForUserAsync(long userId, int limit, CancellationToken ct = default)
    {
        return await _db.Notifications
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default)
    {
        return await _db.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .CountAsync(ct);
    }

    public async Task<bool> MarkReadAsync(long userId, long notificationId, CancellationToken ct = default)
    {
        var notif = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId, ct);

        if (notif == null) return false;

        notif.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MarkAllReadAsync(long userId, CancellationToken ct = default)
    {
        var unreadNotifs = await _db.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ToListAsync(ct);

        if (!unreadNotifs.Any()) return true;

        foreach (var n in unreadNotifs)
        {
            n.IsRead = true;
        }
        
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default)
    {
        _db.Notifications.AddRange(notifications);
        await _db.SaveChangesAsync(ct);
    }
}
