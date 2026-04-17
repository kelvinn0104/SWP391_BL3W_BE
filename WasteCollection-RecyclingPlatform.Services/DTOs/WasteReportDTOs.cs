using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WasteCollection_RecyclingPlatform.Services.DTOs;

public class WasteReportCreateRequest
{
    public string? Title { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    public string? LocationText { get; set; }
    public long? WardId { get; set; }
    public long? AreaId { get; set; }

    public long? WasteCategoryId { get; set; }
    public decimal? EstimatedWeightKg { get; set; }
    public List<WasteReportItemCreateRequest> WasteItems { get; set; } = new();

    public List<IFormFile> Images { get; set; } = new();

    public List<WasteReportItemCreateRequest> GetWasteItems()
    {
        if (WasteItems.Count > 0) return WasteItems;
        if (!WasteCategoryId.HasValue) return new List<WasteReportItemCreateRequest>();

        return new List<WasteReportItemCreateRequest>
        {
            new()
            {
                WasteCategoryId = WasteCategoryId.Value,
                EstimatedWeightKg = EstimatedWeightKg,
            },
        };
    }
}

public class WasteReportItemCreateRequest
{
    [Required]
    public long WasteCategoryId { get; set; }

    public decimal? EstimatedWeightKg { get; set; }
}

public class WasteCategoryResponse
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public string? Description { get; set; }
    public int PointsPerKg { get; set; }
}

public class WasteReportResponse
{
    public long ReportId { get; set; }
    public long CitizenId { get; set; }
    public string? Title { get; set; }
    public string Description { get; set; } = null!;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? LocationText { get; set; }
    public long? WardId { get; set; }
    public long? AreaId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public List<WasteReportItemResponse> WasteItems { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();
    public int EstimatedTotalPoints { get; set; }
}

public class WasteReportItemResponse
{
    public long WasteCategoryId { get; set; }
    public string WasteCategoryCode { get; set; } = null!;
    public string WasteCategoryName { get; set; } = null!;
    public decimal? EstimatedWeightKg { get; set; }
    public int EstimatedPoints { get; set; }
}

public class WasteReportCreateResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public WasteReportResponse? Report { get; set; }

    public static WasteReportCreateResult Fail(string error) => new() { Success = false, Error = error };
    public static WasteReportCreateResult Ok(WasteReportResponse report) => new() { Success = true, Report = report };
}
