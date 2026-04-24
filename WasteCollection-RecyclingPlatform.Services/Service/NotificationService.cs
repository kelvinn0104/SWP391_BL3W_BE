using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Repositories.Repository;
using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<List<NotificationResponse>> GetNotificationsAsync(long userId, int limit = 50, CancellationToken ct = default)
    {
        var notifications = await _notificationRepository.GetForUserAsync(userId, limit, ct);
        return notifications.Select(n => new NotificationResponse
        {
            Id = n.Id,
            Title = n.Title,
            Body = n.Body,
            Type = n.Type.ToString(),
            RelatedReportId = n.RelatedReportId,
            IsRead = n.IsRead,
            CreatedAtUtc = n.CreatedAtUtc
        }).ToList();
    }

    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default)
    {
        return await _notificationRepository.GetUnreadCountAsync(userId, ct);
    }

    public async Task<bool> MarkReadAsync(long userId, long notificationId, CancellationToken ct = default)
    {
        return await _notificationRepository.MarkReadAsync(userId, notificationId, ct);
    }

    public async Task<bool> MarkAllReadAsync(long userId, CancellationToken ct = default)
    {
        return await _notificationRepository.MarkAllReadAsync(userId, ct);
    }

    public async Task NotifyReportCreatedAsync(long reportId, string citizenName, IEnumerable<long> enterpriseUserIds, CancellationToken ct = default)
    {
        var notifications = enterpriseUserIds.Select(adminId => new Notification
        {
            RecipientUserId = adminId,
            Title = "🔔 Yêu cầu thu gom mới",
            Body = $"Công dân {citizenName} vừa tạo một yêu cầu thu gom mới (#{reportId}). Hệ thống cần điều phối nhân viên.",
            Type = NotificationType.ReportCreated,
            RelatedReportId = reportId
        }).ToList();

        if (notifications.Any())
        {
            await _notificationRepository.AddRangeAsync(notifications, ct);
        }
    }

    public async Task NotifyCollectorAssignedAsync(long reportId, long collectorId, string citizenAddress, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            RecipientUserId = collectorId,
            Title = "📋 Nhiệm vụ thu gom mới",
            Body = $"Bạn vừa được phân công một đơn thu gom mới (#{reportId}) tại địa chỉ: {citizenAddress}.",
            Type = NotificationType.CollectorAssigned,
            RelatedReportId = reportId
        };
        await _notificationRepository.AddAsync(notification, ct);
    }

    public async Task NotifyCollectorAcceptedAsync(long reportId, IEnumerable<long> enterpriseUserIds, long citizenId, CancellationToken ct = default)
    {
        var notifications = new List<Notification>();

        foreach (var adminId in enterpriseUserIds)
        {
            notifications.Add(new Notification
            {
                RecipientUserId = adminId,
                Title = "✅ Đơn đã được tiếp nhận",
                Body = $"Nhân viên đã tiếp nhận thông tin và đang trên đường đến thu gom đơn #{reportId}.",
                Type = NotificationType.CollectorAccepted,
                RelatedReportId = reportId
            });
        }

        notifications.Add(new Notification
        {
            RecipientUserId = citizenId,
            Title = "🛵 Nhân viên đang đến",
            Body = $"Nhân viên thu gom đã tiếp nhận đơn #{reportId} của bạn và đang trên đường đến.",
            Type = NotificationType.CollectorAccepted,
            RelatedReportId = reportId
        });

        if (notifications.Any())
        {
            await _notificationRepository.AddRangeAsync(notifications, ct);
        }
    }

    public async Task NotifyReportCollectedAsync(long reportId, IEnumerable<long> enterpriseUserIds, long citizenId, decimal points, CancellationToken ct = default)
    {
         var notifications = new List<Notification>();

        foreach (var adminId in enterpriseUserIds)
        {
            notifications.Add(new Notification
            {
                RecipientUserId = adminId,
                Title = "📦 Thu gom hoàn tất",
                Body = $"Đơn thu gom #{reportId} đã được hoàn tất thành công.",
                Type = NotificationType.ReportCollected,
                RelatedReportId = reportId
            });
        }

        notifications.Add(new Notification
        {
            RecipientUserId = citizenId,
            Title = "🎉 Thu gom thành công!",
            Body = $"Rác tái chế của bạn (Đơn #{reportId}) đã được thu gom thành công. Bạn nhận được {points:N0} điểm thưởng!",
            Type = NotificationType.ReportCollected,
            RelatedReportId = reportId
        });

        if (notifications.Any())
        {
            await _notificationRepository.AddRangeAsync(notifications, ct);
        }
    }

    public async Task NotifyReportCancelledAsync(long reportId, long citizenId, string reason, CancellationToken ct = default)
    {
         var notification = new Notification
        {
            RecipientUserId = citizenId,
            Title = "❌ Đơn thu gom đã bị hủy",
            Body = $"Đơn thu gom #{reportId} của bạn đã bị hủy với lý do: {reason}",
            Type = NotificationType.ReportCancelled,
            RelatedReportId = reportId
        };
        await _notificationRepository.AddAsync(notification, ct);
    }

    public async Task NotifyComplaintSubmittedAsync(long complaintId, long reportId, string citizenName, IEnumerable<long> adminUserIds, CancellationToken ct = default)
    {
        var notifications = adminUserIds.Select(adminId => new Notification
        {
            RecipientUserId = adminId,
            Title = "⚠️ Có khiếu nại mới",
            Body = $"Công dân {citizenName} vừa gửi khiếu nại (#{complaintId}) liên quan đến báo cáo thu gom #{reportId}.",
            Type = NotificationType.ComplaintSubmitted,
            RelatedReportId = reportId
        }).ToList();

        if (notifications.Any())
        {
            await _notificationRepository.AddRangeAsync(notifications, ct);
        }
    }

    public async Task NotifyComplaintStatusUpdatedAsync(long complaintId, long reportId, long citizenId, string newStatus, string? note, CancellationToken ct = default)
    {
        string statusText = newStatus switch
        {
            "InReview" => "Đang xử lý",
            "Resolved" => "Đã giải quyết",
            "Rejected" => "Bị từ chối",
            _ => "Đã cập nhật",
        };

        var notification = new Notification
        {
            RecipientUserId = citizenId,
            Title = $"📝 Cập nhật khiếu nại #{complaintId}",
            Body = $"Khiếu nại của bạn ở trạng thái: {statusText}.{(string.IsNullOrWhiteSpace(note) ? "" : $" Ghi chú: {note}")}",
            Type = NotificationType.ComplaintStatusUpdated,
            RelatedReportId = reportId
        };
        
        await _notificationRepository.AddAsync(notification, ct);
    }

    public async Task NotifyCollectorRejectedAsync(long reportId, string? collectorName, IEnumerable<long> enterpriseUserIds, string? reason, CancellationToken ct = default)
    {
        var notifications = enterpriseUserIds.Select(adminId => new Notification
        {
            RecipientUserId = adminId,
            Title = "⚠️ Nhân viên từ chối đơn thu gom",
            Body = $"Nhân viên {collectorName ?? "thu gom"} đã từ chối đơn #{reportId}. Lý do: {reason ?? "Không rõ"}. Đơn đã được chuyển lại về trạng thái Đang chờ (Pending).",
            Type = NotificationType.CollectorAssigned,
            RelatedReportId = reportId
        }).ToList();

        if (notifications.Any())
        {
            await _notificationRepository.AddRangeAsync(notifications, ct);
        }
    }
}
