namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class WasteReportStatusHistory
{
    public long Id { get; set; }
    public long WasteReportId { get; set; }
    public WasteReport WasteReport { get; set; } = null!;

    public WasteReportStatus Status { get; set; } = WasteReportStatus.Pending;
    public string? Note { get; set; }
    public long? ChangedByUserId { get; set; }
    public User? ChangedByUser { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
