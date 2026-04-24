using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public interface INotificationService
{
    Task<List<NotificationResponse>> GetNotificationsAsync(long userId, int limit = 50, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default);
    Task<bool> MarkReadAsync(long userId, long notificationId, CancellationToken ct = default);
    Task<bool> MarkAllReadAsync(long userId, CancellationToken ct = default);

    Task NotifyReportCreatedAsync(long reportId, string citizenName, IEnumerable<long> enterpriseUserIds, CancellationToken ct = default);
    Task NotifyCollectorAssignedAsync(long reportId, long collectorId, string citizenAddress, CancellationToken ct = default);
    Task NotifyCollectorAcceptedAsync(long reportId, IEnumerable<long> enterpriseUserIds, long citizenId, CancellationToken ct = default);
    Task NotifyReportCollectedAsync(long reportId, IEnumerable<long> enterpriseUserIds, long citizenId, decimal points, CancellationToken ct = default);
    Task NotifyReportCancelledAsync(long reportId, long citizenId, string reason, long? collectorId = null, CancellationToken ct = default);
    Task NotifyComplaintSubmittedAsync(long complaintId, long reportId, string citizenName, IEnumerable<long> adminUserIds, CancellationToken ct = default);
    Task NotifyComplaintStatusUpdatedAsync(long complaintId, long reportId, long citizenId, string newStatus, string? note, CancellationToken ct = default);
    Task NotifyCollectorRejectedAsync(long reportId, string? collectorName, IEnumerable<long> enterpriseUserIds, string? reason, CancellationToken ct = default);
    Task NotifyCollectorUnassignedAsync(long reportId, long collectorId, CancellationToken ct = default);
}
