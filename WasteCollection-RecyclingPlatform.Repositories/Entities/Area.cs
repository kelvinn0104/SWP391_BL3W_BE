namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class Area
{
    public long Id { get; set; }
    public string DistrictName { get; set; } = null!;
    public decimal MonthlyCapacityKg { get; set; }
    public decimal ProcessedThisMonthKg { get; set; }
    public int CompletedRequests { get; set; }

    public ICollection<Ward> Wards { get; set; } = new List<Ward>();
}
