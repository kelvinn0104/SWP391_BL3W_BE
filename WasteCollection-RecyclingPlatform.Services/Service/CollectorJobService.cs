using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Repositories.Repository;
using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public class CollectorJobService : ICollectorJobService
{
    private readonly IWasteReportRepository _wasteReportRepository;
    private readonly IUserRepository _userRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CollectorJobService(
        IWasteReportRepository wasteReportRepository,
        IUserRepository userRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _wasteReportRepository = wasteReportRepository;
        _userRepository = userRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<CollectorJobListResult> GetMyJobsAsync(long collectorId, WasteReportStatus? status, CancellationToken ct = default)
    {
        if (status.HasValue && !Enum.IsDefined(status.Value))
            return CollectorJobListResult.Fail("Invalid report status. Valid values: Pending, Accepted, Assigned, Collected, Cancelled.");

        var reports = await _wasteReportRepository.GetAssignedToCollectorAsync(collectorId, status, ct);
        return CollectorJobListResult.Ok(reports.Select(MapJobSummary).ToList());
    }

    public async Task<CollectorJobDetailResult> GetMyJobDetailAsync(long collectorId, long reportId, CancellationToken ct = default)
    {
        var report = await _wasteReportRepository.GetAssignedDetailForCollectorAsync(collectorId, reportId, ct);
        return report is null
            ? CollectorJobDetailResult.NotFoundResult()
            : CollectorJobDetailResult.Ok(MapJob(report));
    }

    public async Task<CollectorJobDetailResult> AssignCollectorAsync(long actorUserId, long reportId, long collectorId, CancellationToken ct = default)
    {
        var collector = await _userRepository.GetByIdAsync(collectorId, ct);
        if (collector is null || collector.Role != UserRole.Collector)
            return CollectorJobDetailResult.Fail("Collector khong ton tai hoac khong dung vai tro Collector.");

        var report = await _wasteReportRepository.GetByIdForAssignmentAsync(reportId, ct);
        if (report is null)
            return CollectorJobDetailResult.NotFoundResult();

        var now = DateTime.UtcNow;
        report.AssignedCollectorId = collectorId;
        report.AssignedCollector = collector;
        report.AssignedAtUtc = now;
        report.Status = WasteReportStatus.Assigned;
        report.UpdatedAtUtc = now;
        report.StatusHistories.Add(new WasteReportStatusHistory
        {
            Status = WasteReportStatus.Assigned,
            ChangedByUserId = actorUserId,
            ChangedAtUtc = now,
            Note = $"Assigned to collector #{collectorId}.",
        });

        await _wasteReportRepository.SaveChangesAsync(ct);

        var saved = await _wasteReportRepository.GetByIdAsync(reportId, ct);
        return saved is null
            ? CollectorJobDetailResult.Fail("Khong the doc lai cong viec sau khi phan cong.")
            : CollectorJobDetailResult.Ok(MapJob(saved));
    }

    public bool TryGetCurrentUserId(ClaimsPrincipal user, out long userId)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst("id")?.Value;

        return long.TryParse(raw, out userId);
    }

    private CollectorJobResponse MapJob(WasteReport report)
    {
        var imageUrls = report.Images.Select(x => ToClientImageUrl(x.ImageUrl)).ToList();

        return new CollectorJobResponse
        {
            Id = report.Id,
            ReportId = report.Id,
            Title = report.Title,
            Description = report.Description,
            WeightKg = GetTotalWeightKg(report),
            Category = GetCategoryLabel(report),
            Location = report.LocationText,
            CreatedAt = ToFeDate(report.CreatedAtUtc),
            LocationText = report.LocationText,
            Status = ToFeStatus(report.Status),
            CreatedAtUtc = report.CreatedAtUtc,
            AssignedAtUtc = report.AssignedAtUtc,
            Citizen = new CollectorJobCitizenResponse
            {
                Id = report.CitizenId,
                FullName = report.Citizen?.FullName ?? report.Citizen?.DisplayName,
                PhoneNumber = report.Citizen?.PhoneNumber,
            },
            WasteItems = report.Items.Select(x => new CollectorJobWasteItemResponse
            {
                WasteCategoryId = x.WasteCategoryId,
                WasteCategoryCode = x.WasteCategory?.Code ?? string.Empty,
                WasteCategoryName = x.WasteCategory?.Name ?? string.Empty,
                EstimatedWeightKg = x.EstimatedWeightKg,
                EstimatedPoints = x.EstimatedPoints,
                ImageUrls = x.Images.Select(image => ToClientImageUrl(image.ImageUrl)).ToList(),
            }).ToList(),
            Images = imageUrls,
            ImageUrls = imageUrls,
        };
    }

    private static CollectorJobSummaryResponse MapJobSummary(WasteReport report)
    {
        return new CollectorJobSummaryResponse
        {
            Id = report.Id,
            ReportId = report.Id,
            Title = report.Title,
            Description = report.Description,
            WeightKg = GetTotalWeightKg(report),
            Category = GetCategoryLabel(report),
            Location = report.LocationText,
            CreatedAt = ToFeDate(report.CreatedAtUtc),
            LocationText = report.LocationText,
            Status = ToFeStatus(report.Status),
            CreatedAtUtc = report.CreatedAtUtc,
            AssignedAtUtc = report.AssignedAtUtc,
            Citizen = new CollectorJobCitizenResponse
            {
                Id = report.CitizenId,
                FullName = report.Citizen?.FullName ?? report.Citizen?.DisplayName,
                PhoneNumber = report.Citizen?.PhoneNumber,
            },
        };
    }

    private static string ToFeDate(DateTime value)
    {
        return value.ToString("yyyy-MM-dd");
    }

    private static decimal? GetTotalWeightKg(WasteReport report)
    {
        var weights = report.Items
            .Select(x => x.EstimatedWeightKg)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();

        return weights.Count == 0 ? null : weights.Sum();
    }

    private static string GetCategoryLabel(WasteReport report)
    {
        var categoryNames = report.Items
            .Select(x => x.WasteCategory?.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return categoryNames.Count == 0 ? "Chưa phân loại" : string.Join(", ", categoryNames);
    }

    private static string ToFeStatus(WasteReportStatus status)
    {
        return status switch
        {
            WasteReportStatus.Pending => "Chờ duyệt",
            WasteReportStatus.Accepted => "Đã duyệt",
            WasteReportStatus.Assigned => "Đã phân công",
            WasteReportStatus.Collected => "Đã thu gom",
            WasteReportStatus.Cancelled => "Đã hủy",
            _ => status.ToString(),
        };
    }

    private string ToClientImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return imageUrl;

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
            return imageUrl;

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is null)
            return imageUrl;

        var imagePath = imageUrl.StartsWith("/", StringComparison.Ordinal)
            ? imageUrl
            : $"/{imageUrl}";

        return $"{request.Scheme}://{request.Host}{request.PathBase}{imagePath}";
    }
}
