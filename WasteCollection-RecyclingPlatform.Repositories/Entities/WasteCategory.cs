namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class WasteCategory
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Unit { get; set; } = "kg";
    public string? Description { get; set; }
    public int PointsPerKg { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<WasteReportItem> ReportItems { get; set; } = new List<WasteReportItem>();
}
