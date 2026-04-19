namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class ComplaintEvidence
{
    public long Id { get; set; }
    public long ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;

    public string FileUrl { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
