namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class WasteReportImage
{
    public long Id { get; set; }
    public long WasteReportId { get; set; }
    public WasteReport WasteReport { get; set; } = null!;

    public long? WasteReportItemId { get; set; }
    public WasteReportItem? WasteReportItem { get; set; }

    public string ImageUrl { get; set; } = null!;
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public string Purpose { get; set; } = WasteReportImagePurpose.ReportEvidence;
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class WasteReportImagePurpose
{
    public const string ReportEvidence = "ReportEvidence";
    public const string CompletionProof = "CompletionProof";
}
