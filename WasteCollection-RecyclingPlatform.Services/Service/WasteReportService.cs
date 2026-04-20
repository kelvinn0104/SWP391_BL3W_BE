using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Repositories.Repository;
using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public class WasteReportService : IWasteReportService
{
    private const int MaxImagesPerReport = 10;
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

    public WasteReportService(
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

    public async Task<List<WasteCategoryResponse>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _wasteReportRepository.GetActiveCategoriesAsync(ct);
        return categories.Select(MapCategory).ToList();
    }

    public async Task<List<WasteReportResponse>> GetReportsAsync(CancellationToken ct = default)
    {
        var reports = await _wasteReportRepository.GetAllAsync(ct);
        return reports.Select(MapReport).ToList();
    }

    public async Task<List<WasteReportResponse>> GetCitizenReportsAsync(long citizenId, CancellationToken ct = default)
    {
        var reports = await _wasteReportRepository.GetByCitizenIdAsync(citizenId, ct);
        return reports.Select(MapReport).ToList();
    }

    public async Task<List<WasteReportResponse>?> SearchCitizenReportsByStatusAsync(long citizenId, WasteReportStatus status, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(status))
            return null;

        var reports = await _wasteReportRepository.GetByCitizenIdAndStatusAsync(citizenId, status, ct);
        return reports.Select(MapReport).ToList();
    }

    public async Task<List<WasteReportResponse>?> SearchReportsByStatusAsync(long currentUserId, bool canViewAllReports, WasteReportStatus status, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(status))
            return null;

        var reports = canViewAllReports
            ? await _wasteReportRepository.GetByStatusAsync(status, ct)
            : await _wasteReportRepository.GetByCitizenIdAndStatusAsync(currentUserId, status, ct);

        return reports.Select(MapReport).ToList();
    }

    public async Task<WasteReportResponse?> GetCitizenReportDetailAsync(long citizenId, long reportId, CancellationToken ct = default)
    {
        var report = await _wasteReportRepository.GetByIdAsync(reportId, ct);
        if (report is null || report.CitizenId != citizenId) return null;

        return MapReport(report);
    }

    public async Task<WasteReportStatusTrackingResponse?> GetCitizenReportStatusAsync(long citizenId, long reportId, CancellationToken ct = default)
    {
        var report = await _wasteReportRepository.GetStatusTrackingByIdAsync(reportId, ct);
        if (report is null || report.CitizenId != citizenId) return null;

        return MapStatusTracking(report);
    }

    public async Task<WasteReportStatusTrackingResponse?> GetReportStatusTrackingAsync(long reportId, CancellationToken ct = default)
    {
        var report = await _wasteReportRepository.GetStatusTrackingByIdAsync(reportId, ct);
        return report is null ? null : MapStatusTracking(report);
    }

    public WasteReportFormBindResult BindWasteItemsFromRawForm(WasteReportCreateRequest request, IFormCollection? form)
    {
        if (form is null)
            return WasteReportFormBindResult.Ok();

        if (request.WasteCategoryIds.Count == 0)
        {
            BindPrimitiveListFromForm(form, "WasteCategoryIds", request.WasteCategoryIds);
            BindPrimitiveListFromForm(form, "wasteCategoryIds", request.WasteCategoryIds);
            BindIndexedPrimitiveListFromForm(form, "WasteCategoryIds", request.WasteCategoryIds);
            BindIndexedPrimitiveListFromForm(form, "wasteCategoryIds", request.WasteCategoryIds);
        }

        if (request.EstimatedWeightKgs.Count == 0)
        {
            BindPrimitiveListFromForm(form, "EstimatedWeightKgs", request.EstimatedWeightKgs);
            BindPrimitiveListFromForm(form, "estimatedWeightKgs", request.EstimatedWeightKgs);
            BindIndexedPrimitiveListFromForm(form, "EstimatedWeightKgs", request.EstimatedWeightKgs);
            BindIndexedPrimitiveListFromForm(form, "estimatedWeightKgs", request.EstimatedWeightKgs);
        }

        if (request.WasteCategoryIds.Count > 0)
            return WasteReportFormBindResult.Ok();

        foreach (var rawWasteItems in form["WasteItems"].Concat(form["wasteItems"]))
        {
            if (string.IsNullOrWhiteSpace(rawWasteItems)) continue;

            try
            {
                using var document = JsonDocument.Parse(rawWasteItems);
                BindRawWasteItems(request, document.RootElement);

                if (request.WasteCategoryIds.Count > 0)
                    return WasteReportFormBindResult.Ok();
            }
            catch
            {
                // Legacy WasteItems is only a compatibility fallback.
            }
        }

        return WasteReportFormBindResult.Ok();
    }

    public bool TryGetCurrentUserId(ClaimsPrincipal user, out long userId)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst("id")?.Value;

        return long.TryParse(raw, out userId);
    }

    public async Task<WasteReportCreateResult> CreateReportAsync(long citizenId, WasteReportCreateRequest request, CancellationToken ct = default)
    {
        var citizen = await _userRepository.GetByIdAsync(citizenId, ct);
        if (citizen is null) return WasteReportCreateResult.Fail("Không tìm thấy người dùng.");
        if (citizen.Role != UserRole.Citizen) return WasteReportCreateResult.Fail("Chỉ công dân mới được tạo báo cáo thu gom.");

        var description = request.Description?.Trim();
        if (string.IsNullOrWhiteSpace(description)) return WasteReportCreateResult.Fail("Mô tả là bắt buộc.");
        var requestedItems = request.GetWasteItems();
        if (requestedItems.Count == 0) return WasteReportCreateResult.Fail("Cần chọn ít nhất một loại rác.");
        if (requestedItems.Any(x => x.WasteCategoryId <= 0)) return WasteReportCreateResult.Fail("Loại rác không hợp lệ.");
        if (requestedItems.Any(x => x.EstimatedWeightKg < 0)) return WasteReportCreateResult.Fail("Khối lượng ước tính không được âm.");

        var categoryIds = requestedItems.Select(x => x.WasteCategoryId).Distinct().ToList();
        if (categoryIds.Count != requestedItems.Count) return WasteReportCreateResult.Fail("Không gửi trùng loại rác trong cùng một báo cáo.");

        var categories = await _wasteReportRepository.GetActiveCategoriesByIdsAsync(categoryIds, ct);
        if (categories.Count != categoryIds.Count) return WasteReportCreateResult.Fail("Một hoặc nhiều loại rác không tồn tại hoặc đã bị tắt.");

        var totalImageCount = requestedItems.Sum(x => x.Images.Count);
        if (totalImageCount > MaxImagesPerReport)
            return WasteReportCreateResult.Fail($"Chỉ được tải tối đa {MaxImagesPerReport} ảnh.");

        var now = DateTime.UtcNow;
        var categoryById = categories.ToDictionary(x => x.Id);
        var report = new WasteReport
        {
            CitizenId = citizenId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            Description = description,
            LocationText = string.IsNullOrWhiteSpace(request.LocationText) ? null : request.LocationText.Trim(),
            Status = WasteReportStatus.Pending,
            CreatedAtUtc = now,
        };

        try
        {
            foreach (var item in requestedItems)
            {
                var category = categoryById[item.WasteCategoryId];
                var estimatedPoints = _rewardService.CalculateEstimatedPoints(item.EstimatedWeightKg, category.PointsPerKg);
                var reportItem = new WasteReportItem
                {
                    WasteCategoryId = item.WasteCategoryId,
                    EstimatedWeightKg = item.EstimatedWeightKg,
                    EstimatedPoints = estimatedPoints,
                };

                foreach (var image in item.Images.Where(x => x.Length > 0))
                {
                    var imageUrl = await SaveReportImageAsync(image, ct);
                    var reportImage = new WasteReportImage
                    {
                        WasteReport = report,
                        WasteReportItem = reportItem,
                        ImageUrl = imageUrl,
                        OriginalFileName = Path.GetFileName(image.FileName),
                        ContentType = image.ContentType,
                        UploadedAtUtc = now,
                    };

                    report.Images.Add(reportImage);
                    reportItem.Images.Add(reportImage);
                }

                report.Items.Add(reportItem);
            }
        }
        catch (InvalidOperationException ex)
        {
            return WasteReportCreateResult.Fail(ex.Message);
        }

        report.EstimatedTotalPoints = report.Items.Sum(x => x.EstimatedPoints);
        report.StatusHistories.Add(new WasteReportStatusHistory
        {
            Status = WasteReportStatus.Pending,
            ChangedByUserId = citizenId,
            ChangedAtUtc = now,
            Note = "Công dân đã tạo báo cáo rác.",
        });

        await _wasteReportRepository.AddAsync(report, ct);

        var saved = await _wasteReportRepository.GetByIdAsync(report.Id, ct);
        return WasteReportCreateResult.Ok(MapReport(saved ?? report));
    }

    public async Task<WasteReportUpdateResult> UpdateReportAsync(long citizenId, long reportId, WasteReportUpdateRequest request, CancellationToken ct = default)
    {
        var report = await _wasteReportRepository.GetByIdForUpdateAsync(reportId, ct);
        if (report is null || report.CitizenId != citizenId)
            return WasteReportUpdateResult.NotFoundResult();

        if (report.Status != WasteReportStatus.Pending)
            return WasteReportUpdateResult.Fail("Chỉ được cập nhật báo cáo khi trạng thái còn Pending.");

        var description = request.Description?.Trim();
        if (string.IsNullOrWhiteSpace(description)) return WasteReportUpdateResult.Fail("Mô tả là bắt buộc.");

        var requestedItems = request.GetWasteItems();
        if (requestedItems.Count == 0) return WasteReportUpdateResult.Fail("Cần chọn ít nhất một loại rác.");
        if (requestedItems.Any(x => x.WasteCategoryId <= 0)) return WasteReportUpdateResult.Fail("Loại rác không hợp lệ.");
        if (requestedItems.Any(x => x.EstimatedWeightKg < 0)) return WasteReportUpdateResult.Fail("Khối lượng ước tính không được âm.");

        var categoryIds = requestedItems.Select(x => x.WasteCategoryId).Distinct().ToList();
        if (categoryIds.Count != requestedItems.Count) return WasteReportUpdateResult.Fail("Không gửi trùng loại rác trong cùng một báo cáo.");

        var categories = await _wasteReportRepository.GetActiveCategoriesByIdsAsync(categoryIds, ct);
        if (categories.Count != categoryIds.Count) return WasteReportUpdateResult.Fail("Một hoặc nhiều loại rác không tồn tại hoặc đã bị tắt.");

        var totalImageCount = requestedItems.Sum(x => x.Images.Count);
        if (totalImageCount > MaxImagesPerReport)
            return WasteReportUpdateResult.Fail($"Chỉ được tải tối đa {MaxImagesPerReport} ảnh.");

        var now = DateTime.UtcNow;
        var categoryById = categories.ToDictionary(x => x.Id);

        report.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        report.Description = description;
        report.LocationText = string.IsNullOrWhiteSpace(request.LocationText) ? null : request.LocationText.Trim();
        report.UpdatedAtUtc = now;

        report.Images.Clear();
        report.Items.Clear();

        try
        {
            foreach (var item in requestedItems)
            {
                var category = categoryById[item.WasteCategoryId];
                var estimatedPoints = _rewardService.CalculateEstimatedPoints(item.EstimatedWeightKg, category.PointsPerKg);
                var reportItem = new WasteReportItem
                {
                    WasteCategoryId = item.WasteCategoryId,
                    EstimatedWeightKg = item.EstimatedWeightKg,
                    EstimatedPoints = estimatedPoints,
                };

                foreach (var image in item.Images.Where(x => x.Length > 0))
                {
                    var imageUrl = await SaveReportImageAsync(image, ct);
                    var reportImage = new WasteReportImage
                    {
                        WasteReport = report,
                        WasteReportItem = reportItem,
                        ImageUrl = imageUrl,
                        OriginalFileName = Path.GetFileName(image.FileName),
                        ContentType = image.ContentType,
                        UploadedAtUtc = now,
                    };

                    report.Images.Add(reportImage);
                    reportItem.Images.Add(reportImage);
                }

                report.Items.Add(reportItem);
            }
        }
        catch (InvalidOperationException ex)
        {
            return WasteReportUpdateResult.Fail(ex.Message);
        }

        report.EstimatedTotalPoints = report.Items.Sum(x => x.EstimatedPoints);
        await _wasteReportRepository.SaveChangesAsync(ct);

        var saved = await _wasteReportRepository.GetByIdAsync(report.Id, ct);
        return WasteReportUpdateResult.Ok(MapReport(saved ?? report));
    }

    public async Task<WasteReportStatusChangeResult> AdvanceReportStatusAsync(long actorUserId, long reportId, string? note, CancellationToken ct = default)
    {
        var report = await _wasteReportRepository.GetByIdForUpdateAsync(reportId, ct);
        if (report is null)
            return WasteReportStatusChangeResult.NotFoundResult();

        return WasteReportStatusChangeResult.Fail("Luồng trạng thái hiện tại không dùng advance-status để chuyển report sang Accepted. Admin/Enterprise duyệt và phân công bằng API assign-collector để chuyển Pending -> Assigned, hoặc dùng API cancel để chuyển Pending -> Cancelled. Collector sẽ chuyển Assigned -> Accepted khi đồng ý nhận việc.");
    }

    public async Task<WasteReportStatusChangeResult> CancelReportAsync(long actorUserId, long reportId, string? note, CancellationToken ct = default)
    {
        var report = await _wasteReportRepository.GetByIdForUpdateAsync(reportId, ct);
        if (report is null)
            return WasteReportStatusChangeResult.NotFoundResult();

        if (report.Status == WasteReportStatus.Cancelled)
            return WasteReportStatusChangeResult.Fail("Báo cáo đã ở trạng thái Cancelled.");

        if (report.Status != WasteReportStatus.Pending)
            return WasteReportStatusChangeResult.Fail($"Chỉ có thể không duyệt report từ trạng thái Pending. Trạng thái hiện tại là {report.Status}.");

        var now = DateTime.UtcNow;
        report.Status = WasteReportStatus.Cancelled;
        report.UpdatedAtUtc = now;
        report.StatusHistories.Add(new WasteReportStatusHistory
        {
            Status = WasteReportStatus.Cancelled,
            ChangedByUserId = actorUserId,
            ChangedAtUtc = now,
            Note = string.IsNullOrWhiteSpace(note)
                ? "Báo cáo đã bị hủy."
                : note.Trim(),
        });

        await _wasteReportRepository.SaveChangesAsync(ct);

        var trackedReport = await _wasteReportRepository.GetStatusTrackingByIdAsync(report.Id, ct);
        if (trackedReport is null)
            return WasteReportStatusChangeResult.Fail("Không thể đọc lại trạng thái sau khi cập nhật.");

        return WasteReportStatusChangeResult.Ok(MapStatusTracking(trackedReport));
    }

    private static WasteCategoryResponse MapCategory(WasteCategory category)
    {
        return new WasteCategoryResponse
        {
            Id = category.Id,
            Code = category.Code,
            Name = category.Name,
            Unit = category.Unit,
            Description = category.Description,
            PointsPerKg = category.PointsPerKg,
        };
    }

    private WasteReportResponse MapReport(WasteReport report)
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

    private WasteReportStatusTrackingResponse MapStatusTracking(WasteReport report)
    {
        var histories = report.StatusHistories
            .OrderBy(x => x.ChangedAtUtc)
            .ThenBy(x => x.Id)
            .ToList();

        var historyResponses = histories.Select(x => new WasteReportStatusHistoryResponse
        {
            Id = x.Id,
            Status = x.Status.ToString(),
            Note = x.Note,
            ChangedByUserId = x.ChangedByUserId,
            ChangedByName = x.ChangedByUser?.DisplayName ?? x.ChangedByUser?.FullName,
            ChangedByRole = x.ChangedByUser?.Role.ToString(),
            ChangedAtUtc = x.ChangedAtUtc,
        }).ToList();

        if (historyResponses.Count == 0)
        {
            historyResponses.Add(new WasteReportStatusHistoryResponse
            {
                Status = report.Status.ToString(),
                ChangedByUserId = report.CitizenId,
                ChangedAtUtc = report.CreatedAtUtc,
                Note = "Ảnh chụp trạng thái hiện tại.",
            });
        }

        var assignedHistory = histories
            .Where(x => x.Status == WasteReportStatus.Assigned)
            .OrderByDescending(x => x.ChangedAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();

        return new WasteReportStatusTrackingResponse
        {
            ReportId = report.Id,
            CurrentStatus = report.Status.ToString(),
            CreatedAtUtc = report.CreatedAtUtc,
            UpdatedAtUtc = report.UpdatedAtUtc,
            PendingAtUtc = GetFirstStatusAt(histories, WasteReportStatus.Pending) ?? report.CreatedAtUtc,
            AcceptedAtUtc = GetFirstStatusAt(histories, WasteReportStatus.Accepted),
            AssignedAtUtc = assignedHistory?.ChangedAtUtc ?? report.AssignedAtUtc,
            CollectedAtUtc = GetFirstStatusAt(histories, WasteReportStatus.Collected),
            CancelledAtUtc = GetFirstStatusAt(histories, WasteReportStatus.Cancelled),
            ActualTotalWeightKg = report.ActualTotalWeightKg,
            CompletedAtUtc = report.CompletedAtUtc,
            CompletionNote = report.CompletionNote,
            Assignment = MapAssignment(report),
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
            ProofImageUrls = report.Images
                .Where(x => x.Purpose == WasteReportImagePurpose.CompletionProof)
                .Select(x => ToClientImageUrl(x.ImageUrl))
                .ToList(),
            StatusHistory = historyResponses,
        };
    }

    private static DateTime? GetFirstStatusAt(IEnumerable<WasteReportStatusHistory> histories, WasteReportStatus status)
    {
        return histories
            .Where(x => x.Status == status)
            .OrderBy(x => x.ChangedAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => (DateTime?)x.ChangedAtUtc)
            .FirstOrDefault();
    }

    private static WasteReportAssignmentInfoResponse? MapAssignment(WasteReport report)
    {
        if (!report.AssignedCollectorId.HasValue || !report.AssignedAtUtc.HasValue)
            return null;

        return new WasteReportAssignmentInfoResponse
        {
            CollectorId = report.AssignedCollectorId.Value,
            CollectorName = report.AssignedCollector?.DisplayName ?? report.AssignedCollector?.FullName,
            CollectorPhone = report.AssignedCollector?.PhoneNumber,
            AssignedAtUtc = report.AssignedAtUtc.Value,
        };
    }

    private static void BindRawWasteItems(WasteReportCreateRequest request, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var itemElement in element.EnumerateArray())
            {
                BindRawWasteItems(request, itemElement);
            }

            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return;

        if (TryGetJsonProperty(element, "wasteItems", out var wasteItemsElement))
        {
            BindRawWasteItems(request, wasteItemsElement);
            return;
        }

        if (!TryGetJsonProperty(element, "wasteCategoryId", out var categoryIdElement)
            || !TryGetLong(categoryIdElement, out var categoryId))
        {
            return;
        }

        request.WasteCategoryIds.Add(categoryId);

        if (TryGetJsonProperty(element, "estimatedWeightKg", out var weightElement)
            && TryGetDecimal(weightElement, out var estimatedWeightKg))
        {
            request.EstimatedWeightKgs.Add(estimatedWeightKg);
        }
        else
        {
            request.EstimatedWeightKgs.Add(null);
        }
    }

    private static void BindPrimitiveListFromForm<T>(IFormCollection form, string key, List<T> target)
    {
        foreach (var rawValue in form[key])
        {
            AddPrimitiveValues(rawValue, target);
        }
    }

    private static void BindIndexedPrimitiveListFromForm<T>(IFormCollection form, string key, List<T> target)
    {
        var prefix = key + "[";
        var values = form.Keys
            .Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(x => new
            {
                Key = x,
                Index = TryReadIndex(x, prefix.Length, out var index) ? index : int.MaxValue,
            })
            .OrderBy(x => x.Index)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .SelectMany(x => form[x.Key]);

        foreach (var rawValue in values)
        {
            AddPrimitiveValues(rawValue, target);
        }
    }

    private static void AddPrimitiveValues<T>(string? rawValue, List<T> target)
    {
        if (string.IsNullOrWhiteSpace(rawValue)) return;

        try
        {
            if (rawValue.TrimStart().StartsWith("[", StringComparison.Ordinal))
            {
                var values = JsonSerializer.Deserialize<List<T>>(rawValue, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (values is not null) target.AddRange(values);
                return;
            }

            foreach (var value in rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                target.Add((T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)));
            }
        }
        catch
        {
            // Business validation will report missing or invalid categories.
        }
    }

    private static bool TryGetJsonProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetLong(JsonElement element, out long value)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt64(out value);

        if (element.ValueKind == JsonValueKind.String)
            return long.TryParse(element.GetString(), out value);

        value = default;
        return false;
    }

    private static bool TryGetDecimal(JsonElement element, out decimal value)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetDecimal(out value);

        if (element.ValueKind == JsonValueKind.String)
            return decimal.TryParse(element.GetString(), out value);

        value = default;
        return false;
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

    private static async Task<string> SaveReportImageAsync(IFormFile file, CancellationToken ct)
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

