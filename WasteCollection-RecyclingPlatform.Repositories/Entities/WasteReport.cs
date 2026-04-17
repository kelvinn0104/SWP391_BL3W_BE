namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class WasteReport
{
    public long Id { get; set; }
    public long CitizenId { get; set; }
    public User Citizen { get; set; } = null!;

    public string? Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? LocationText { get; set; }

    public long? WardId { get; set; }
    public Ward? Ward { get; set; }
    public long? AreaId { get; set; }
    public Area? Area { get; set; }

    public WasteReportStatus Status { get; set; } = WasteReportStatus.Pending;
    public int EstimatedTotalPoints { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<WasteReportItem> Items { get; set; } = new List<WasteReportItem>();
    public ICollection<WasteReportImage> Images { get; set; } = new List<WasteReportImage>();
    public ICollection<WasteReportStatusHistory> StatusHistories { get; set; } = new List<WasteReportStatusHistory>();
}
