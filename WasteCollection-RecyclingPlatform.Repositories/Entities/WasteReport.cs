namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class WasteReport
{
    public long Id { get; set; }
    public long CitizenId { get; set; }
    public User Citizen { get; set; } = null!;

    public string? Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? LocationText { get; set; }

    public long? AssignedCollectorId { get; set; }
    public User? AssignedCollector { get; set; }
    public DateTime? AssignedAtUtc { get; set; }

    public WasteReportStatus Status { get; set; } = WasteReportStatus.Pending;
    public int EstimatedTotalPoints { get; set; }
    public int? FinalRewardPoints { get; set; }
    public DateTime? RewardVerifiedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CompletionNote { get; set; }
    public decimal? ActualTotalWeightKg { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public long? WardId { get; set; }
    public Ward? Ward { get; set; }

    public ICollection<WasteReportItem> Items { get; set; } = new List<WasteReportItem>();
    public ICollection<WasteReportImage> Images { get; set; } = new List<WasteReportImage>();
    public ICollection<WasteReportStatusHistory> StatusHistories { get; set; } = new List<WasteReportStatusHistory>();
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
