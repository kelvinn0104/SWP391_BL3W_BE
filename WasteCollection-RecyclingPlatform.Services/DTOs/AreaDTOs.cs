namespace WasteCollection_RecyclingPlatform.Services.DTOs;

public class AreaResponse
{
    public string Id { get; set; } = null!; // Changed to string to match FE mock 'area-1' style if needed, or just stringified long
    public string District { get; set; } = null!;
    public decimal MonthlyCapacityKg { get; set; }
    public decimal ProcessedThisMonthKg { get; set; }
    public int CompletedRequests { get; set; }
    public List<WardResponse> Wards { get; set; } = new List<WardResponse>();
}

public class WardResponse
{
    public string Name { get; set; } = null!;
    public List<string> Collectors { get; set; } = new List<string>();
    public decimal CollectedKg { get; set; }
    public int CompletedRequests { get; set; }
}

public class AreaBulkUpdateRequest
{
    public List<AreaResponse> Areas { get; set; } = new List<AreaResponse>();
}
