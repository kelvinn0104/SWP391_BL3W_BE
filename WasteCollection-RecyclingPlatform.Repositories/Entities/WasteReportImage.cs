namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class WasteReportImage
{
    public long Id { get; set; }
    public long WasteReportId { get; set; }
    public WasteReport WasteReport { get; set; } = null!;

    public string ImageUrl { get; set; } = null!;
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
