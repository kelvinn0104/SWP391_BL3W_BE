using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Repositories.Repository;
using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public class CollectorJobService : ICollectorJobService
{
    private const int MaxCompletionProofImages = 10;
    private const long MaxImageBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
    };

    private readonly IWasteReportRepository _wasteReportRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRewardService _rewardService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INotificationService _notificationService;

    public CollectorJobService(
        IWasteReportRepository wasteReportRepository,
        IUserRepository userRepository,
        IRewardService rewardService,
        IHttpContextAccessor httpContextAccessor,
        INotificationService notificationService)
    {
        _wasteReportRepository = wasteReportRepository;
        _userRepository = userRepository;
        _rewardService = rewardService;
        _httpContextAccessor = httpContextAccessor;
        _notificationService = notificationService;
    }

    public async Task<CollectorJobListResult> GetMyJobsAsync(long collectorId, WasteReportStatus? status, CancellationToken ct = default)
    {
        if (status.HasValue && !Enum.IsDefined(status.Value))
            return CollectorJobListResult.Fail("Trạng thái báo cáo không hợp lệ. Các giá trị hợp lệ: Pending, Accepted, Assigned, Collected, Cancelled.");

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
            return CollectorJobDetailResult.Fail("Collector không tồn tại hoặc không đúng vai trò Collector.");

        var report = await _wasteReportRepository.GetByIdForAssignmentAsync(reportId, ct);
        if (report is null)
            return CollectorJobDetailResult.NotFoundResult();

        if (report.Status != WasteReportStatus.Pending && report.Status != WasteReportStatus.Assigned)
            return CollectorJobDetailResult.Fail($"Chỉ có thể duyệt và phân công report từ trạng thái Pending hoặc cập nhật phân công từ Assigned. Trạng thái hiện tại là {report.Status}.");

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
            Note = $"Đã phân công cho collector #{collectorId}.",
        });

        await _wasteReportRepository.SaveChangesAsync(ct);

        await _notificationService.NotifyCollectorAssignedAsync(reportId, collectorId, report.LocationText ?? report.Citizen?.PhoneNumber ?? "địa chỉ chưa cập nhật", ct);

        var saved = await _wasteReportRepository.GetByIdAsync(reportId, ct);
        return saved is null
            ? CollectorJobDetailResult.Fail("Không thể đọc lại công việc sau khi phân công.")
            : CollectorJobDetailResult.Ok(MapJob(saved));
    }

    public async Task<CollectorJobDetailResult> AcceptMyJobAsync(long collectorId, long reportId, string? note, CancellationToken ct = default)
    {
        var report = await _wasteReportRepository.GetAssignedForCollectorUpdateAsync(collectorId, reportId, ct);
        if (report is null)
            return CollectorJobDetailResult.NotFoundResult();

        if (report.Status != WasteReportStatus.Assigned)
            return CollectorJobDetailResult.Fail($"Chỉ có thể đồng ý nhận công việc từ trạng thái Assigned. Trạng thái hiện tại là {report.Status}.");

        var now = DateTime.UtcNow;
        report.Status = WasteReportStatus.Accepted;
        report.UpdatedAtUtc = now;
        report.StatusHistories.Add(new WasteReportStatusHistory
        {
            Status = WasteReportStatus.Accepted,
            ChangedByUserId = collectorId,
            ChangedAtUtc = now,
            Note = string.IsNullOrWhiteSpace(note)
                ? "Collector đã đồng ý nhận công việc."
                : note.Trim(),
        });

        await _wasteReportRepository.SaveChangesAsync(ct);

        var enterprises = await _userRepository.GetByRoleAsync(UserRole.RecyclingEnterprise, null, ct);
        var enterpriseIds = enterprises.Select(x => x.Id).ToList();
        await _notificationService.NotifyCollectorAcceptedAsync(report.Id, enterpriseIds, report.CitizenId, ct);

        var saved = await _wasteReportRepository.GetByIdAsync(reportId, ct);
        return saved is null
            ? CollectorJobDetailResult.Fail("Không thể đọc lại công việc sau khi cập nhật trạng thái.")
            : CollectorJobDetailResult.Ok(MapJob(saved));
    }

    public async Task<CollectorJobDetailResult> CancelMyJobAsync(long collectorId, long reportId, string? note, CancellationToken ct = default)
    {
        var report = await _wasteReportRepository.GetAssignedForCollectorUpdateAsync(collectorId, reportId, ct);
        if (report is null)
            return CollectorJobDetailResult.NotFoundResult();

        if (report.Status != WasteReportStatus.Assigned)
            return CollectorJobDetailResult.Fail($"Chỉ có thể từ chối công việc từ trạng thái Assigned. Trạng thái hiện tại là {report.Status}.");

        var now = DateTime.UtcNow;
        report.AssignedCollectorId = null;
        report.AssignedCollector = null;
        report.AssignedAtUtc = null;
        report.Status = WasteReportStatus.Pending;
        report.UpdatedAtUtc = now;
        report.StatusHistories.Add(new WasteReportStatusHistory
        {
            Status = WasteReportStatus.Pending,
            ChangedByUserId = collectorId,
            ChangedAtUtc = now,
            Note = string.IsNullOrWhiteSpace(note)
                ? "Collector đã từ chối công việc, report quay lại Pending để enterprise phân công collector khác."
                : note.Trim(),
        });

        await _wasteReportRepository.SaveChangesAsync(ct);

        var enterprises = await _userRepository.GetByRoleAsync(UserRole.RecyclingEnterprise, null, ct);
        var enterpriseIds = enterprises.Select(x => x.Id).ToList();
        var collector = await _userRepository.GetByIdAsync(collectorId, ct);
        await _notificationService.NotifyCollectorRejectedAsync(reportId, collector?.DisplayName ?? collector?.Email, enterpriseIds, note, ct);

        var saved = await _wasteReportRepository.GetByIdAsync(reportId, ct);
        return saved is null
            ? CollectorJobDetailResult.Fail("Không thể đọc lại công việc sau khi từ chối.")
            : CollectorJobDetailResult.Ok(MapJob(saved));
    }

    public CollectorJobFormBindResult BindCompletionRequestFromRawForm(CollectorJobCompletionRequest request, IFormCollection? form)
    {
        if (form is not null)
            BindParallelArraysFromForm(form, request);

        return ValidateParallelArrays(request);
    }

    public async Task<CollectorJobCompletionResult> CompleteMyJobAsync(long collectorId, long reportId, CollectorJobCompletionRequest request, CancellationToken ct = default)
    {
        var report = await _wasteReportRepository.GetAssignedForCollectorUpdateAsync(collectorId, reportId, ct);
        if (report is null)
            return CollectorJobCompletionResult.NotFoundResult();

        if (report.Status != WasteReportStatus.Accepted)
            return CollectorJobCompletionResult.Fail($"Không thể xác nhận hoàn tất từ trạng thái {report.Status}. Luồng hợp lệ là Pending -> Assigned -> Accepted -> Collected.");

        var proofImages = request.ProofImages.Where(x => x.Length > 0).ToList();
        if (proofImages.Count == 0)
            return CollectorJobCompletionResult.Fail("Vui lòng tải lên ít nhất một ảnh minh chứng hoàn tất thu gom.");

        if (proofImages.Count > MaxCompletionProofImages)
            return CollectorJobCompletionResult.Fail($"Chỉ được tải tối đa {MaxCompletionProofImages} ảnh minh chứng.");

        var bindResult = ValidateParallelArrays(request);
        if (!bindResult.Success)
            return CollectorJobCompletionResult.Fail(bindResult.Error ?? "Dữ liệu đầu vào không hợp lệ.");

        var actualItemsResult = await BuildActualWasteItemsAsync(request, report, ct);
        if (!actualItemsResult.Success)
            return CollectorJobCompletionResult.Fail(actualItemsResult.Error ?? "Dữ liệu khối lượng thực tế không hợp lệ.");

        if (actualItemsResult.Items.Any(x => x.ActualWeightKg < 0))
            return CollectorJobCompletionResult.Fail("Khối lượng thực tế theo từng loại rác không được âm.");

        var duplicateReportCategoryIds = report.Items
            .GroupBy(x => x.WasteCategoryId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicateReportCategoryIds.Count > 0)
            return CollectorJobCompletionResult.Fail($"Report đang có loại rác bị trùng: {string.Join(", ", duplicateReportCategoryIds)}.");

        ApplyActualWasteItems(report, actualItemsResult.Items);
        report.ActualTotalWeightKg = report.Items.Sum(x => x.ActualWeightKg ?? 0);

        var note = request.CompletionNote?.Trim();

        var now = DateTime.UtcNow;
        report.Status = WasteReportStatus.Collected;
        report.CompletedAtUtc = request.CompletedAtUtc ?? now;
        report.CompletionNote = string.IsNullOrWhiteSpace(note) ? null : note;
        report.UpdatedAtUtc = now;

        try
        {
            foreach (var image in proofImages)
            {
                var imageUrl = await SaveCompletionProofImageAsync(image, ct);
                report.Images.Add(new WasteReportImage
                {
                    WasteReport = report,
                    ImageUrl = imageUrl,
                    OriginalFileName = Path.GetFileName(image.FileName),
                    ContentType = image.ContentType,
                    Purpose = WasteReportImagePurpose.CompletionProof,
                    UploadedAtUtc = now,
                });
            }
        }
        catch (InvalidOperationException ex)
        {
            return CollectorJobCompletionResult.Fail(ex.Message);
        }

        report.StatusHistories.Add(new WasteReportStatusHistory
        {
            Status = WasteReportStatus.Collected,
            ChangedByUserId = collectorId,
            ChangedAtUtc = now,
            Note = string.IsNullOrWhiteSpace(note)
                ? "Collector đã xác nhận hoàn tất thu gom."
                : note,
        });

        var pointsPerKgByCategoryId = actualItemsResult.Items.ToDictionary(x => x.CategoryId, x => x.PointsPerKg);
        await _rewardService.AwardFinalPointsForCollectedReportAsync(report, collectorId, pointsPerKgByCategoryId, ct);
        await _wasteReportRepository.SaveChangesAsync(ct);

        var enterprises = await _userRepository.GetByRoleAsync(UserRole.RecyclingEnterprise, null, ct);
        var enterpriseIds = enterprises.Select(x => x.Id).ToList();
        await _notificationService.NotifyReportCollectedAsync(report.Id, enterpriseIds, report.CitizenId, (decimal)(report.FinalRewardPoints ?? 0), ct);

        var saved = await _wasteReportRepository.GetByIdAsync(reportId, ct);
        return saved is null
            ? CollectorJobCompletionResult.Fail("Không thể đọc lại công việc sau khi xác nhận hoàn tất.")
            : CollectorJobCompletionResult.Ok(MapWasteReport(saved));
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
        var proofImageUrls = report.Images
            .Where(x => x.Purpose == WasteReportImagePurpose.CompletionProof)
            .Select(x => ToClientImageUrl(x.ImageUrl))
            .ToList();
        var wasteItems = report.Items.Select(x => new CollectorJobWasteItemResponse
        {
            WasteReportItemId = x.Id,
            WasteCategoryId = x.WasteCategoryId,
            WasteCategoryCode = x.WasteCategory?.Code ?? string.Empty,
            WasteCategoryName = x.WasteCategory?.Name ?? string.Empty,
            EstimatedWeightKg = x.EstimatedWeightKg,
            ActualWeightKg = x.ActualWeightKg,
            EstimatedPoints = x.EstimatedPoints,
            ImageUrls = x.Images
                .Where(image => image.Purpose == WasteReportImagePurpose.ReportEvidence)
                .Select(image => ToClientImageUrl(image.ImageUrl))
                .ToList(),
        }).ToList();

        return new CollectorJobResponse
        {
            Id = report.Id,
            ReportId = report.Id,
            Title = report.Title,
            Description = report.Description,
            WeightKg = GetDisplayWeightKg(report),
            Category = GetCategoryLabel(report),
            Location = report.LocationText,
            CreatedAt = ToFeDate(report.CreatedAtUtc),
            LocationText = report.LocationText,
            Status = report.Status.ToString(),
            CreatedAtUtc = report.CreatedAtUtc,
            AssignedAtUtc = report.AssignedAtUtc,
            CompletedAtUtc = report.CompletedAtUtc,
            CompletionNote = report.CompletionNote,
            ActualTotalWeightKg = report.ActualTotalWeightKg,
            Citizen = new CollectorJobCitizenResponse
            {
                Id = report.CitizenId,
                FullName = report.Citizen?.FullName ?? report.Citizen?.DisplayName,
                PhoneNumber = report.Citizen?.PhoneNumber,
            },
            WasteItems = wasteItems,
            Images = imageUrls,
            ImageUrls = imageUrls,
            ProofImageUrls = proofImageUrls,
        };
    }

    private WasteReportResponse MapWasteReport(WasteReport report)
    {
        var reportEvidenceUrls = report.Images
            .Where(x => x.Purpose == WasteReportImagePurpose.ReportEvidence)
            .Select(x => ToClientImageUrl(x.ImageUrl))
            .ToList();
        var proofImageUrls = report.Images
            .Where(x => x.Purpose == WasteReportImagePurpose.CompletionProof)
            .Select(x => ToClientImageUrl(x.ImageUrl))
            .ToList();
        var wasteItems = report.Items.Select(x => new WasteReportItemResponse
        {
            WasteReportItemId = x.Id,
            WasteCategoryId = x.WasteCategoryId,
            WasteCategoryCode = x.WasteCategory?.Code ?? string.Empty,
            WasteCategoryName = x.WasteCategory?.Name ?? string.Empty,
            EstimatedWeightKg = x.EstimatedWeightKg,
            ActualWeightKg = x.ActualWeightKg,
            EstimatedPoints = x.EstimatedPoints,
            ImageUrls = x.Images
                .Where(image => image.Purpose == WasteReportImagePurpose.ReportEvidence)
                .Select(image => ToClientImageUrl(image.ImageUrl))
                .ToList(),
        }).ToList();
        var calculatedFinalPoints = CalculateActualTotalPoints(report);

        return new WasteReportResponse
        {
            ReportId = report.Id,
            CitizenId = report.CitizenId,
            Title = report.Title,
            Description = report.Description,
            LocationText = report.LocationText,
            Status = report.Status.ToString(),
            CreatedAtUtc = report.CreatedAtUtc,
            EstimatedTotalPoints = report.EstimatedTotalPoints,
            FinalRewardPoints = report.Status == WasteReportStatus.Collected ? calculatedFinalPoints : report.FinalRewardPoints,
            RewardVerifiedAtUtc = report.RewardVerifiedAtUtc,
            ActualTotalWeightKg = report.ActualTotalWeightKg,
            CompletedAtUtc = report.CompletedAtUtc,
            CompletionNote = report.CompletionNote,
            WasteItems = wasteItems,
            ImageUrls = reportEvidenceUrls,
            ProofImageUrls = proofImageUrls,
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
            WeightKg = GetDisplayWeightKg(report),
            Category = GetCategoryLabel(report),
            Location = report.LocationText,
            CreatedAt = ToFeDate(report.CreatedAtUtc),
            LocationText = report.LocationText,
            Status = report.Status.ToString(),
            CreatedAtUtc = report.CreatedAtUtc,
            AssignedAtUtc = report.AssignedAtUtc,
            CompletedAtUtc = report.CompletedAtUtc,
            ActualTotalWeightKg = report.ActualTotalWeightKg,
            Citizen = new CollectorJobCitizenResponse
            {
                Id = report.CitizenId,
                FullName = report.Citizen?.FullName ?? report.Citizen?.DisplayName,
                PhoneNumber = report.Citizen?.PhoneNumber,
            },
        };
    }

    private int CalculateActualPoints(WasteReportItem item)
    {
        if (!item.ActualWeightKg.HasValue || item.WasteCategory is null)
            return 0;

        return _rewardService.CalculateEstimatedPoints(item.ActualWeightKg, item.WasteCategory.PointsPerKg);
    }

    private int CalculateActualTotalPoints(WasteReport report)
    {
        return report.Items.Sum(CalculateActualPoints);
    }

    private static string ToFeDate(DateTime value)
    {
        return value.ToString("yyyy-MM-dd");
    }

    private static decimal? GetDisplayWeightKg(WasteReport report)
    {
        return report.ActualTotalWeightKg ?? GetEstimatedTotalWeightKg(report);
    }

    private static decimal? GetEstimatedTotalWeightKg(WasteReport report)
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

    private string ToClientImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return imageUrl;

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
            return imageUrl;

        return imageUrl.StartsWith("/", StringComparison.Ordinal)
            ? imageUrl
            : $"/{imageUrl}";
    }

    private static CollectorJobFormBindResult ValidateParallelArrays(CollectorJobCompletionRequest request)
    {
        var hasCategoryIds = request.WasteCategoryIds.Count > 0;
        var hasCategoryNames = request.CategoryNames.Count > 0;
        var hasWeights = request.ActualWeightKgs.Count > 0;

        if (hasCategoryIds && hasCategoryNames)
            return CollectorJobFormBindResult.Fail("Chỉ gửi một trong hai: WasteCategoryIds hoặc CategoryNames.");

        if (!hasCategoryIds && !hasCategoryNames && !hasWeights)
            return CollectorJobFormBindResult.Ok();

        if (hasCategoryIds && (!hasWeights || request.WasteCategoryIds.Count != request.ActualWeightKgs.Count))
            return CollectorJobFormBindResult.Fail("Số lượng WasteCategoryIds phải bằng số lượng ActualWeightKgs.");

        if (hasCategoryNames && (!hasWeights || request.CategoryNames.Count != request.ActualWeightKgs.Count))
            return CollectorJobFormBindResult.Fail("Số lượng CategoryNames phải bằng số lượng ActualWeightKgs.");

        return CollectorJobFormBindResult.Ok();
    }

    private async Task<ActualWasteItemsResult> BuildActualWasteItemsAsync(CollectorJobCompletionRequest request, WasteReport report, CancellationToken ct)
    {
        var reportItems = report.Items.OrderBy(x => x.Id).ToList();
        if (request.WasteCategoryIds.Count > 0)
        {
            if (request.WasteCategoryIds.Any(x => x <= 0))
                return ActualWasteItemsResult.Fail("WasteCategoryIds không hợp lệ.");

            var duplicateCategoryIds = request.WasteCategoryIds
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();
            if (duplicateCategoryIds.Count > 0)
                return ActualWasteItemsResult.Fail($"Không gửi trùng WasteCategoryIds: {string.Join(", ", duplicateCategoryIds)}.");

            var categories = await _wasteReportRepository.GetActiveCategoriesByIdsAsync(request.WasteCategoryIds, ct);
            if (categories.Count != request.WasteCategoryIds.Distinct().Count())
                return ActualWasteItemsResult.Fail("Một hoặc nhiều loại rác thực tế không tồn tại hoặc đã bị tắt.");

            var categoryById = categories.ToDictionary(x => x.Id);
            return ActualWasteItemsResult.Ok(request.WasteCategoryIds.Select((categoryId, index) => new ActualWasteCategoryWeight
            {
                CategoryId = categoryId,
                PointsPerKg = categoryById[categoryId].PointsPerKg,
                ActualWeightKg = request.ActualWeightKgs[index],
            }).ToList());
        }

        if (request.CategoryNames.Count > 0)
        {
            var normalizedNames = request.CategoryNames.Select(NormalizeCategoryName).ToList();
            var duplicateCategoryNames = normalizedNames
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();
            if (duplicateCategoryNames.Count > 0)
                return ActualWasteItemsResult.Fail($"Không gửi trùng CategoryNames: {string.Join(", ", duplicateCategoryNames)}.");

            var activeCategories = await _wasteReportRepository.GetActiveCategoriesAsync(ct);
            var categoryGroupsByName = activeCategories
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => NormalizeCategoryName(x.Name))
                .ToDictionary(x => x.Key, x => x.ToList());

            var result = new List<ActualWasteCategoryWeight>(request.CategoryNames.Count);
            for (var i = 0; i < request.CategoryNames.Count; i++)
            {
                var categoryName = request.CategoryNames[i];
                var normalizedCategoryName = NormalizeCategoryName(categoryName);
                if (!categoryGroupsByName.TryGetValue(normalizedCategoryName, out var matchedCategories) || matchedCategories.Count == 0)
                    return ActualWasteItemsResult.Fail($"CategoryName '{categoryName}' không tồn tại hoặc đã bị tắt.");

                if (matchedCategories.Count > 1)
                    return ActualWasteItemsResult.Fail($"CategoryName '{categoryName}' bị trùng trong cấu hình loại rác. Vui lòng dùng WasteCategoryIds.");

                result.Add(new ActualWasteCategoryWeight
                {
                    CategoryId = matchedCategories[0].Id,
                    PointsPerKg = matchedCategories[0].PointsPerKg,
                    ActualWeightKg = request.ActualWeightKgs[i],
                });
            }

            return ActualWasteItemsResult.Ok(result);
        }

        if (request.ActualWeightKgs.Count == 0)
            return ActualWasteItemsResult.Fail("Vui lòng nhập khối lượng thực tế cho từng loại rác.");

        if (request.ActualWeightKgs.Count != reportItems.Count)
            return ActualWasteItemsResult.Fail("Số lượng ActualWeightKgs phải bằng số loại rác dự kiến khi không gửi WasteCategoryIds/CategoryNames.");

        if (reportItems.Any(x => x.WasteCategory is null))
            return ActualWasteItemsResult.Fail("Không thể cập nhật khối lượng vì report thiếu thông tin loại rác.");

        return ActualWasteItemsResult.Ok(reportItems.Select((item, index) => new ActualWasteCategoryWeight
        {
            CategoryId = item.WasteCategoryId,
            PointsPerKg = item.WasteCategory!.PointsPerKg,
            ActualWeightKg = request.ActualWeightKgs[index],
        }).ToList());
    }

    private static void ApplyActualWasteItems(WasteReport report, List<ActualWasteCategoryWeight> actualItems)
    {
        var existingItemsByCategoryId = report.Items.ToDictionary(x => x.WasteCategoryId);
        var actualCategoryIds = actualItems.Select(x => x.CategoryId).ToHashSet();

        foreach (var item in report.Items)
        {
            if (!actualCategoryIds.Contains(item.WasteCategoryId))
                item.ActualWeightKg = 0;
        }

        foreach (var actualItem in actualItems)
        {
            if (existingItemsByCategoryId.TryGetValue(actualItem.CategoryId, out var reportItem))
            {
                reportItem.ActualWeightKg = actualItem.ActualWeightKg;
                continue;
            }

            report.Items.Add(new WasteReportItem
            {
                WasteReport = report,
                WasteCategoryId = actualItem.CategoryId,
                EstimatedWeightKg = null,
                ActualWeightKg = actualItem.ActualWeightKg,
                EstimatedPoints = 0,
            });
        }
    }

    private static void BindParallelArraysFromForm(IFormCollection form, CollectorJobCompletionRequest request)
    {
        if (request.WasteCategoryIds.Count == 0)
        {
            request.WasteCategoryIds = ReadLongListFromForm(
                form,
                "WasteCategoryIds",
                "wasteCategoryIds",
                "WasteCategoryId",
                "wasteCategoryId",
                "CategoryIds",
                "categoryIds");
        }

        if (request.CategoryNames.Count == 0)
        {
            request.CategoryNames = ReadStringListFromForm(
                form,
                "CategoryNames",
                "categoryNames",
                "CategoryName",
                "categoryName");
        }

        if (request.ActualWeightKgs.Count == 0)
        {
            request.ActualWeightKgs = ReadDecimalListFromForm(
                form,
                "ActualWeightKgs",
                "actualWeightKgs",
                "ActualWeights",
                "actualWeights");
        }
    }

    private static List<long> ReadLongListFromForm(IFormCollection form, params string[] keys)
    {
        var values = ReadStringListFromForm(form, keys);
        var result = new List<long>(values.Count);

        foreach (var value in values)
        {
            if (long.TryParse(value, out var parsed))
                result.Add(parsed);
        }

        return result;
    }

    private static List<decimal> ReadDecimalListFromForm(IFormCollection form, params string[] keys)
    {
        var values = ReadStringListFromForm(form, keys);
        var result = new List<decimal>(values.Count);

        foreach (var value in values)
        {
            if (decimal.TryParse(value, out var parsed))
                result.Add(parsed);
        }

        return result;
    }

    private static string NormalizeCategoryName(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static List<string> ReadStringListFromForm(IFormCollection form, params string[] keys)
    {
        var result = new List<string>();

        foreach (var key in keys)
        {
            foreach (var value in form[key])
            {
                if (!string.IsNullOrWhiteSpace(value))
                    result.Add(value);
            }

            var indexedValues = form.Keys
                .Where(x => x.StartsWith(key + "[", StringComparison.OrdinalIgnoreCase))
                .Select(x => new
                {
                    Key = x,
                    HasIndex = TryReadIndex(x, key.Length + 1, out var index),
                    Index = index,
                })
                .Where(x => x.HasIndex)
                .OrderBy(x => x.Index)
                .Select(x => form[x.Key].FirstOrDefault())
                .Where(x => !string.IsNullOrWhiteSpace(x));

            result.AddRange(indexedValues!);
        }

        return result;
    }

    private static bool TryReadIndex(string key, int startIndex, out int index)
    {
        var endIndex = key.IndexOf(']', startIndex);
        if (endIndex <= startIndex)
        {
            index = default;
            return false;
        }

        return int.TryParse(key[startIndex..endIndex], out index);
    }

    private sealed class ActualWasteCategoryWeight
    {
        public long CategoryId { get; set; }
        public int PointsPerKg { get; set; }
        public decimal ActualWeightKg { get; set; }
    }

    private sealed class ActualWasteItemsResult
    {
        public bool Success { get; private init; }
        public string? Error { get; private init; }
        public List<ActualWasteCategoryWeight> Items { get; private init; } = new();

        public static ActualWasteItemsResult Fail(string error) => new() { Success = false, Error = error };
        public static ActualWasteItemsResult Ok(List<ActualWasteCategoryWeight> items) => new() { Success = true, Items = items };
    }

    private static async Task<string> SaveCompletionProofImageAsync(IFormFile file, CancellationToken ct)
    {
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ hỗ trợ tệp hình ảnh.");

        if (file.Length > MaxImageBytes)
            throw new InvalidOperationException("Ảnh không được vượt quá 10MB.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            throw new InvalidOperationException("Định dạng ảnh không được hỗ trợ.");

        var uploadDirectory = ResolveUploadDirectory();
        Directory.CreateDirectory(uploadDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(uploadDirectory, fileName);

        await using var stream = new FileStream(filePath, FileMode.CreateNew);
        await file.CopyToAsync(stream, ct);

        return $"/collector-images/{fileName}";
    }

    private static string ResolveUploadDirectory()
    {
        var staticFilesRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        return Path.Combine(staticFilesRoot, "collector-images");
    }
}
