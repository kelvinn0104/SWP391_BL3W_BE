namespace WasteCollection_RecyclingPlatform.Services.DTOs;

public class NotificationResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long? RelatedReportId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class UnreadNotificationCountResponse
{
    public int Count { get; set; }
}
