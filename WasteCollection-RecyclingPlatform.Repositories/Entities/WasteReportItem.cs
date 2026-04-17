namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class WasteReportItem
{
    public long Id { get; set; }
    public long WasteReportId { get; set; }
    public WasteReport WasteReport { get; set; } = null!;

    public long WasteCategoryId { get; set; }
    public WasteCategory WasteCategory { get; set; } = null!;

    public decimal? EstimatedWeightKg { get; set; }
    public int EstimatedPoints { get; set; }
}
