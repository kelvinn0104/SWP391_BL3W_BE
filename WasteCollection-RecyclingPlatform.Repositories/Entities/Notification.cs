namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public enum NotificationType
{
    ReportCreated,      // Citizen tạo báo cáo → Enterprise
    CollectorAssigned,  // Enterprise phân công → Collector
    CollectorAccepted,  // Collector xác nhận → Enterprise + Citizen
    ReportCollected,    // Collector hoàn thành → Enterprise + Citizen (kèm điểm)
    ReportCancelled,    // Báo cáo bị hủy → Citizen
    ComplaintSubmitted, // Citizen khiếu nại báo cáo → Enterprise/Admin
    ComplaintStatusUpdated, // Admin thay đổi trạng thái khiếu nại → Citizen
}

public class Notification
{
    public long Id { get; set; }

    /// <summary>Người nhận thông báo</summary>
    public long RecipientUserId { get; set; }
    public User RecipientUser { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;

    public NotificationType Type { get; set; }

    /// <summary>ID báo cáo liên quan (dùng để navigate khi click)</summary>
    public long? RelatedReportId { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
