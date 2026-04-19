using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WasteCollection_RecyclingPlatform.Services.DTOs;

public class WasteReportCreateRequest
{
    public string? Title { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? LocationText { get; set; }

    public List<long> WasteCategoryIds { get; set; } = new();
    public List<decimal?> EstimatedWeightKgs { get; set; } = new();
    public List<IFormFile> Images { get; set; } = new();

    public List<WasteReportItemCreateRequest> GetWasteItems()
    {
        var items = new List<WasteReportItemCreateRequest>();

        for (var i = 0; i < WasteCategoryIds.Count; i++)
        {
            var item = new WasteReportItemCreateRequest
            {
                WasteCategoryId = WasteCategoryIds[i],
                EstimatedWeightKg = i < EstimatedWeightKgs.Count ? EstimatedWeightKgs[i] : null,
            };

            if (WasteCategoryIds.Count == 1)
            {
                item.Images.AddRange(Images);
            }
            else if (i < Images.Count)
            {
                item.Images.Add(Images[i]);
            }

            items.Add(item);
        }

        return items;
    }
}

public class WasteReportUpdateRequest : WasteReportCreateRequest
{
}

public class WasteReportItemCreateRequest
{
    [Required]
    public long WasteCategoryId { get; set; }

    public decimal? EstimatedWeightKg { get; set; }
    public List<IFormFile> Images { get; set; } = new();
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
    public string? LocationText { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public List<WasteReportItemResponse> WasteItems { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();
    public int EstimatedTotalPoints { get; set; }
    public int? FinalRewardPoints { get; set; }
    public DateTime? RewardVerifiedAtUtc { get; set; }
    public decimal? ActualTotalWeightKg { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CompletionNote { get; set; }
    public List<string> ProofImageUrls { get; set; } = new();
}

public class WasteReportStatusTrackingResponse
{
    public long ReportId { get; set; }
    public string CurrentStatus { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? PendingAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? AssignedAtUtc { get; set; }
    public DateTime? CollectedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public decimal? ActualTotalWeightKg { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CompletionNote { get; set; }
    public WasteReportAssignmentInfoResponse? Assignment { get; set; }
    public List<WasteReportItemResponse> WasteItems { get; set; } = new();
    public List<string> ProofImageUrls { get; set; } = new();
    public List<WasteReportStatusHistoryResponse> StatusHistory { get; set; } = new();
}

public class WasteReportStatusHistoryResponse
{
    public long Id { get; set; }
    public string Status { get; set; } = null!;
    public string? Note { get; set; }
    public long? ChangedByUserId { get; set; }
    public string? ChangedByName { get; set; }
    public string? ChangedByRole { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}

public class WasteReportAssignmentInfoResponse
{
    public long CollectorId { get; set; }
    public string? CollectorName { get; set; }
    public string? CollectorPhone { get; set; }
    public DateTime AssignedAtUtc { get; set; }
}

public class WasteReportItemResponse
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

public class WasteReportCreateResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public WasteReportResponse? Report { get; set; }

    public static WasteReportCreateResult Fail(string error) => new() { Success = false, Error = error };
    public static WasteReportCreateResult Ok(WasteReportResponse report) => new() { Success = true, Report = report };
}

public class WasteReportUpdateResult
{
    public bool Success { get; set; }
    public bool NotFound { get; set; }
    public string? Error { get; set; }
    public WasteReportResponse? Report { get; set; }

    public static WasteReportUpdateResult Fail(string error) => new() { Success = false, Error = error };
    public static WasteReportUpdateResult NotFoundResult() => new() { Success = false, NotFound = true, Error = "Không tìm thấy báo cáo." };
    public static WasteReportUpdateResult Ok(WasteReportResponse report) => new() { Success = true, Report = report };
}

public class WasteReportStatusActionRequest
{
    public string? Note { get; set; }
}

public class WasteReportStatusChangeResult
{
    public bool Success { get; set; }
    public bool NotFound { get; set; }
    public string? Error { get; set; }
    public WasteReportStatusTrackingResponse? Tracking { get; set; }

    public static WasteReportStatusChangeResult Fail(string error) => new() { Success = false, Error = error };
    public static WasteReportStatusChangeResult NotFoundResult() => new() { Success = false, NotFound = true, Error = "Không tìm thấy báo cáo." };
    public static WasteReportStatusChangeResult Ok(WasteReportStatusTrackingResponse tracking) => new() { Success = true, Tracking = tracking };
}

public class WasteReportFormBindResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }

    public static WasteReportFormBindResult Fail(string error) => new() { Success = false, Error = error };
    public static WasteReportFormBindResult Ok() => new() { Success = true };
}
