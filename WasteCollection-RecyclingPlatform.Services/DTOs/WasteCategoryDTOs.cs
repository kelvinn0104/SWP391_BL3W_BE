using System.ComponentModel.DataAnnotations;

namespace WasteCollection_RecyclingPlatform.Services.DTOs;

// ─── Requests ──────────────────────────────────────────────────────────────

public class WasteCategoryCreateRequest
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Unit { get; set; } = "kg";

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Điểm tích lũy mỗi kg</summary>
    [Range(1, int.MaxValue, ErrorMessage = "PointsPerKg phải lớn hơn 0.")]
    public int PointsPerKg { get; set; } = 100;
}

public class WasteCategoryUpdatePointsRequest
{
    /// <summary>Điểm tích lũy mỗi kg (FE lưu theo 0.1 kg, nhân 10 trước khi gửi lên)</summary>
    [Range(1, int.MaxValue, ErrorMessage = "PointsPerKg phải lớn hơn 0.")]
    public int PointsPerKg { get; set; }
}

// ─── Responses ─────────────────────────────────────────────────────────────

public class WasteCategoryDetailResponse
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public string? Description { get; set; }
    public int PointsPerKg { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

// ─── Results ───────────────────────────────────────────────────────────────

public class WasteCategoryResult
{
    public bool Success { get; set; }
    public bool NotFound { get; set; }
    public string? Error { get; set; }
    public WasteCategoryDetailResponse? Category { get; set; }

    public static WasteCategoryResult Fail(string error) => new() { Success = false, Error = error };
    public static WasteCategoryResult NotFoundResult() => new() { Success = false, NotFound = true, Error = "Không tìm thấy danh mục." };
    public static WasteCategoryResult Ok(WasteCategoryDetailResponse cat) => new() { Success = true, Category = cat };
}
