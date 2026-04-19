using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Services.DTOs;

public class ComplaintCreateRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public List<IFormFile> EvidenceFiles { get; set; } = new();
}

public class ComplaintStatusUpdateRequest
{
    [Required]
    public ComplaintStatus Status { get; set; }

    public string? AdminNote { get; set; }
}

public class ComplaintEvidenceResponse
{
    public long Id { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}

public class ComplaintResponse
{
    public long Id { get; set; }
    public long WasteReportId { get; set; }
    public long CitizenId { get; set; }
    public string? CitizenName { get; set; }
    public string? CitizenEmail { get; set; }
    public string? ReportTitle { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public long? ResolvedByUserId { get; set; }
    public string? ResolvedByName { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public List<ComplaintEvidenceResponse> EvidenceFiles { get; set; } = new();
}

public class ComplaintActionResult<T>
{
    public bool Success { get; set; }
    public bool Unauthorized { get; set; }
    public bool NotFound { get; set; }
    public string? Error { get; set; }
    public T? Data { get; set; }

    public static ComplaintActionResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static ComplaintActionResult<T> Fail(string error) => new() { Error = error };
    public static ComplaintActionResult<T> UnauthorizedResult(string error) => new() { Unauthorized = true, Error = error };
    public static ComplaintActionResult<T> NotFoundResult(string error) => new() { NotFound = true, Error = error };
}
