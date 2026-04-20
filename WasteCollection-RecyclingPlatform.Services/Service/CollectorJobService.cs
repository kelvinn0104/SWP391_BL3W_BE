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

    public CollectorJobService(
        IWasteReportRepository wasteReportRepository,
        IUserRepository userRepository,
        IRewardService rewardService,
        IHttpContextAccessor httpContextAccessor)
    {
        _wasteReportRepository = wasteReportRepository;
        _userRepository = userRepository;
        _rewardService = rewardService;
        _httpContextAccessor = httpContextAccessor;
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

        if (report.Status != WasteReportStatus.Pending)
            return CollectorJobDetailResult.Fail($"Chỉ có thể duyệt và phân công report từ trạng thái Pending. Trạng thái hiện tại là {report.Status}.");

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

        var actualWeightItems = BuildActualWeightItems(request, report);

        if (actualWeightItems.Count == 0)
        {
            if (request.CategoryNames.Count > 0)
                return CollectorJobCompletionResult.Fail("CategoryNames phải trùng với tên loại rác đang có trong report và không được gửi trùng category.");

            return CollectorJobCompletionResult.Fail("Vui lòng nhập khối lượng thực tế cho từng loại rác.");
        }

        if (actualWeightItems.Any(x => x.ActualWeightKg < 0))
            return CollectorJobCompletionResult.Fail("Khối lượng thực tế theo từng loại rác không được âm.");

        var duplicateItemIds = actualWeightItems
            .Where(x => x.WasteReportItemId > 0)
            .GroupBy(x => x.WasteReportItemId)
            .Any(x => x.Count() > 1);
        if (duplicateItemIds)
            return CollectorJobCompletionResult.Fail("Không gửi trùng categoryName khi cập nhật khối lượng thực tế.");

        var itemById = report.Items.ToDictionary(x => x.Id);
        foreach (var itemWeight in actualWeightItems)
        {
            if (!itemById.TryGetValue(itemWeight.WasteReportItemId, out var reportItem))
                return CollectorJobCompletionResult.Fail($"Không tìm thấy wasteReportItemId #{itemWeight.WasteReportItemId} trong công việc này.");

            reportItem.ActualWeightKg = itemWeight.ActualWeightKg;
        }

        var missingActualWeightItemIds = report.Items
            .Where(x => !x.ActualWeightKg.HasValue)
            .Select(x => x.Id)
            .ToList();
        if (missingActualWeightItemIds.Count > 0)
            return CollectorJobCompletionResult.Fail($"Vui lòng nhập đủ khối lượng thực tế cho các wasteReportItemId: {string.Join(", ", missingActualWeightItemIds)}.");

        report.ActualTotalWeightKg = report.Items.Sum(x => x.ActualWeightKg!.Value);

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

        await _rewardService.AwardFinalPointsForCollectedReportAsync(report, collectorId, ct);
        await _wasteReportRepository.SaveChangesAsync(ct);

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
            WasteItems = report.Items.Select(x => new CollectorJobWasteItemResponse
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
            }).ToList(),
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
            FinalRewardPoints = report.FinalRewardPoints,
            RewardVerifiedAtUtc = report.RewardVerifiedAtUtc,
            ActualTotalWeightKg = report.ActualTotalWeightKg,
            CompletedAtUtc = report.CompletedAtUtc,
            CompletionNote = report.CompletionNote,
            WasteItems = report.Items.Select(x => new WasteReportItemResponse
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
            }).ToList(),
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

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is null)
            return imageUrl;

        var imagePath = imageUrl.StartsWith("/", StringComparison.Ordinal)
            ? imageUrl
            : $"/{imageUrl}";

        return $"{request.Scheme}://{request.Host}{request.PathBase}{imagePath}";
    }

    private static CollectorJobFormBindResult ValidateParallelArrays(CollectorJobCompletionRequest request)
    {
        var hasCategoryNames = request.CategoryNames.Count > 0;
        var hasWeights = request.ActualWeightKgs.Count > 0;

        if (!hasCategoryNames && !hasWeights)
            return CollectorJobFormBindResult.Ok();

        if (hasCategoryNames && (!hasWeights || request.CategoryNames.Count != request.ActualWeightKgs.Count))
            return CollectorJobFormBindResult.Fail("Số lượng CategoryNames phải bằng số lượng ActualWeightKgs.");

        return CollectorJobFormBindResult.Ok();
    }

    private static List<CollectorJobItemActualWeightRequest> BuildActualWeightItems(CollectorJobCompletionRequest request, WasteReport report)
    {
        var reportItems = report.Items.OrderBy(x => x.Id).ToList();
        if (request.CategoryNames.Count == 0)
        {
            if (request.ActualWeightKgs.Count != reportItems.Count)
                return new List<CollectorJobItemActualWeightRequest>();

            return reportItems.Select((item, index) => new CollectorJobItemActualWeightRequest
            {
                WasteReportItemId = item.Id,
                ActualWeightKg = request.ActualWeightKgs[index],
            }).ToList();
        }

        var itemsByCategoryName = reportItems
            .Where(x => !string.IsNullOrWhiteSpace(x.WasteCategory?.Name))
            .GroupBy(x => NormalizeCategoryName(x.WasteCategory!.Name))
            .ToDictionary(x => x.Key, x => x.ToList());

        var result = new List<CollectorJobItemActualWeightRequest>(request.CategoryNames.Count);
        for (var i = 0; i < request.CategoryNames.Count; i++)
        {
            var categoryName = request.CategoryNames[i];
            var normalizedCategoryName = NormalizeCategoryName(categoryName);
            if (!itemsByCategoryName.TryGetValue(normalizedCategoryName, out var matchedItems) || matchedItems.Count == 0)
                return new List<CollectorJobItemActualWeightRequest>();

            if (matchedItems.Count > 1)
                return new List<CollectorJobItemActualWeightRequest>();

            result.Add(new CollectorJobItemActualWeightRequest
            {
                WasteReportItemId = matchedItems[0].Id,
                ActualWeightKg = request.ActualWeightKgs[i],
            });
        }

        return result;
    }

    private static void BindParallelArraysFromForm(IFormCollection form, CollectorJobCompletionRequest request)
    {
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

        return $"/report-images/{fileName}";
    }

    private static string ResolveUploadDirectory()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "wwwroot", "report-images"));
    }
}
