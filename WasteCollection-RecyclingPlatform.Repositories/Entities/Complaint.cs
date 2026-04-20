namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class Complaint
{
    public long Id { get; set; }
    public long WasteReportId { get; set; }
    public WasteReport WasteReport { get; set; } = null!;

    public long CitizenId { get; set; }
    public User Citizen { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Submitted;
    public string? AdminNote { get; set; }
    public long? ResolvedByUserId { get; set; }
    public User? ResolvedByUser { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ComplaintEvidence> EvidenceFiles { get; set; } = new List<ComplaintEvidence>();
}
