using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public interface INotificationRepository
{
    Task<List<Notification>> GetForUserAsync(long userId, int limit, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default);
    Task<bool> MarkReadAsync(long userId, long notificationId, CancellationToken ct = default);
    Task<bool> MarkAllReadAsync(long userId, CancellationToken ct = default);
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default);
}
