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

    public WasteReportService(IWasteReportRepository wasteReportRepository, IUserRepository userRepository)
    {
        _wasteReportRepository = wasteReportRepository;
        _userRepository = userRepository;
    }

    public async Task<List<WasteCategoryResponse>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _wasteReportRepository.GetActiveCategoriesAsync(ct);
        return categories.Select(MapCategory).ToList();
    }

    public async Task<List<WasteReportResponse>> GetCitizenReportsAsync(long citizenId, CancellationToken ct = default)
    {
        var reports = await _wasteReportRepository.GetByCitizenIdAsync(citizenId, ct);
        return reports.Select(MapReport).ToList();
    }

    public async Task<List<WasteReportResponse>?> SearchCitizenReportsByStatusAsync(long citizenId, long statusId, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(typeof(WasteReportStatus), (int)statusId))
            return null;

        var status = (WasteReportStatus)(int)statusId;
        var reports = await _wasteReportRepository.GetByCitizenIdAndStatusAsync(citizenId, status, ct);
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
                var estimatedPoints = CalculateEstimatedPoints(item.EstimatedWeightKg, category.PointsPerKg);
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
            Note = "Citizen created waste report.",
        });

        await _wasteReportRepository.AddAsync(report, ct);

        var saved = await _wasteReportRepository.GetByIdAsync(report.Id, ct);
        return WasteReportCreateResult.Ok(MapReport(saved ?? report));
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

    private static WasteReportResponse MapReport(WasteReport report)
    {
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
            WasteItems = report.Items.Select(x => new WasteReportItemResponse
            {
                WasteCategoryId = x.WasteCategoryId,
                WasteCategoryCode = x.WasteCategory?.Code ?? string.Empty,
                WasteCategoryName = x.WasteCategory?.Name ?? string.Empty,
                EstimatedWeightKg = x.EstimatedWeightKg,
                EstimatedPoints = x.EstimatedPoints,
                ImageUrls = x.Images.Select(image => image.ImageUrl).ToList(),
            }).ToList(),
            ImageUrls = report.Images.Select(x => x.ImageUrl).ToList(),
        };
    }

    private static WasteReportStatusTrackingResponse MapStatusTracking(WasteReport report)
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
                Note = "Current status snapshot.",
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
            AssignedAtUtc = assignedHistory?.ChangedAtUtc,
            CollectedAtUtc = GetFirstStatusAt(histories, WasteReportStatus.Collected),
            Assignment = MapAssignment(assignedHistory),
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

    private static WasteReportAssignmentInfoResponse? MapAssignment(WasteReportStatusHistory? assignedHistory)
    {
        if (assignedHistory?.ChangedByUser is null || assignedHistory.ChangedByUser.Role != UserRole.Collector)
            return null;

        return new WasteReportAssignmentInfoResponse
        {
            CollectorId = assignedHistory.ChangedByUser.Id,
            CollectorName = assignedHistory.ChangedByUser.DisplayName ?? assignedHistory.ChangedByUser.FullName,
            CollectorPhone = assignedHistory.ChangedByUser.PhoneNumber,
            AssignedAtUtc = assignedHistory.ChangedAtUtc,
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

    private static int CalculateEstimatedPoints(decimal? estimatedWeightKg, int pointsPerKg)
    {
        if (!estimatedWeightKg.HasValue) return 0;
        return Math.Max(0, (int)Math.Round(estimatedWeightKg.Value * pointsPerKg, MidpointRounding.AwayFromZero));
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
        var workspaceFePublic = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "SWP391_BL3W_FE",
            "public",
            "report-images"));

        var workspaceFeRoot = Path.GetFullPath(Path.Combine(workspaceFePublic, ".."));
        if (Directory.Exists(workspaceFeRoot))
        {
            return workspaceFePublic;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "wwwroot", "report-images"));
    }
}
