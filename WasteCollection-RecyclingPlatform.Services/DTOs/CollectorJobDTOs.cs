using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Services.DTOs;

public class CollectorJobCitizenResponse
{
    public long Id { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
}

public class CollectorJobWasteItemResponse
{
    public long WasteReportItemId { get; set; }
    public long WasteCategoryId { get; set; }
    public string WasteCategoryCode { get; set; } = null!;
    public string WasteCategoryName { get; set; } = null!;
    public decimal? EstimatedWeightKg { get; set; }
    public decimal? ActualWeightKg { get; set; }
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
    public DateTime? CompletedAtUtc { get; set; }
    public string? CompletionNote { get; set; }
    public decimal? ActualTotalWeightKg { get; set; }
    public CollectorJobCitizenResponse Citizen { get; set; } = null!;
    public List<CollectorJobWasteItemResponse> WasteItems { get; set; } = new();
    public List<string> Images { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();
    public List<string> ProofImageUrls { get; set; } = new();
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
    public DateTime? CompletedAtUtc { get; set; }
    public decimal? ActualTotalWeightKg { get; set; }
    public CollectorJobCitizenResponse Citizen { get; set; } = null!;
}

public class AssignWasteReportCollectorRequest
{
    [Required]
    public long CollectorId { get; set; }
}

public class CollectorJobStatusActionRequest
{
    public string? Note { get; set; }
}

public class CollectorJobItemActualWeightRequest
{
    [Required]
    public long WasteReportItemId { get; set; }
    public decimal? ActualWeightKg { get; set; }
}

public class CollectorJobCompletionRequest
{
    public List<IFormFile> ProofImages { get; set; } = new();
    public string? CompletionNote { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public List<long> WasteCategoryIds { get; set; } = new();
    public List<string> CategoryNames { get; set; } = new();
    public List<decimal> ActualWeightKgs { get; set; } = new();
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

public class CollectorJobCompletionResult
{
    public bool Success { get; set; }
    public bool NotFound { get; set; }
    public string? Error { get; set; }
    public WasteReportResponse? Report { get; set; }

    public static CollectorJobCompletionResult Fail(string error) => new() { Success = false, Error = error };
    public static CollectorJobCompletionResult NotFoundResult() => new() { Success = false, NotFound = true, Error = "Không tìm thấy công việc thu gom." };
    public static CollectorJobCompletionResult Ok(WasteReportResponse report) => new() { Success = true, Report = report };
}

public class CollectorJobFormBindResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }

    public static CollectorJobFormBindResult Fail(string error) => new() { Success = false, Error = error };
    public static CollectorJobFormBindResult Ok() => new() { Success = true };
}
