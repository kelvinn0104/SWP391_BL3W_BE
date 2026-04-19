using System.ComponentModel.DataAnnotations;

namespace WasteCollection_RecyclingPlatform.Services.DTOs;

public class CollectorJobCitizenResponse
{
    public long Id { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
}

public class CollectorJobWasteItemResponse
{
    public long WasteCategoryId { get; set; }
    public string WasteCategoryCode { get; set; } = null!;
    public string WasteCategoryName { get; set; } = null!;
    public decimal? EstimatedWeightKg { get; set; }
    public int EstimatedPoints { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}

public class CollectorJobResponse
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public string? Title { get; set; }
    public string Description { get; set; } = null!;
    public decimal? WeightKg { get; set; }
    public string Category { get; set; } = null!;
    public string? Location { get; set; }
    public string CreatedAt { get; set; } = null!;
    public string? LocationText { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? AssignedAtUtc { get; set; }
    public CollectorJobCitizenResponse Citizen { get; set; } = null!;
    public List<CollectorJobWasteItemResponse> WasteItems { get; set; } = new();
    public List<string> Images { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();
}

public class CollectorJobSummaryResponse
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public string? Title { get; set; }
    public string Description { get; set; } = null!;
    public decimal? WeightKg { get; set; }
    public string Category { get; set; } = null!;
    public string? Location { get; set; }
    public string CreatedAt { get; set; } = null!;
    public string? LocationText { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? AssignedAtUtc { get; set; }
    public CollectorJobCitizenResponse Citizen { get; set; } = null!;
}

public class AssignWasteReportCollectorRequest
{
    [Required]
    public long CollectorId { get; set; }
}

public class CollectorJobListResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<CollectorJobSummaryResponse> Jobs { get; set; } = new();

    public static CollectorJobListResult Fail(string error) => new() { Success = false, Error = error };
    public static CollectorJobListResult Ok(List<CollectorJobSummaryResponse> jobs) => new() { Success = true, Jobs = jobs };
}

public class CollectorJobDetailResult
{
    public bool Success { get; set; }
    public bool NotFound { get; set; }
    public string? Error { get; set; }
    public CollectorJobResponse? Job { get; set; }

    public static CollectorJobDetailResult Fail(string error) => new() { Success = false, Error = error };
    public static CollectorJobDetailResult NotFoundResult() => new() { Success = false, NotFound = true, Error = "Không tìm thấy công việc thu gom." };
    public static CollectorJobDetailResult Ok(CollectorJobResponse job) => new() { Success = true, Job = job };
}
