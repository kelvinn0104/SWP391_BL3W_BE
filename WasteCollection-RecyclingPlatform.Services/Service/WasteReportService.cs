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

    public async Task<WasteReportCreateResult> CreateReportAsync(long citizenId, WasteReportCreateRequest request, CancellationToken ct = default)
    {
        var citizen = await _userRepository.GetByIdAsync(citizenId, ct);
        if (citizen is null) return WasteReportCreateResult.Fail("Không tìm thấy người dùng.");
        if (citizen.Role != UserRole.Citizen) return WasteReportCreateResult.Fail("Chỉ công dân mới được tạo báo cáo thu gom.");

        var description = request.Description?.Trim();
        if (string.IsNullOrWhiteSpace(description)) return WasteReportCreateResult.Fail("Mô tả là bắt buộc.");
        if (request.Latitude is < -90 or > 90) return WasteReportCreateResult.Fail("Latitude không hợp lệ.");
        if (request.Longitude is < -180 or > 180) return WasteReportCreateResult.Fail("Longitude không hợp lệ.");

        var requestedItems = request.GetWasteItems();
        if (requestedItems.Count == 0) return WasteReportCreateResult.Fail("Cần chọn ít nhất một loại rác.");
        if (requestedItems.Any(x => x.WasteCategoryId <= 0)) return WasteReportCreateResult.Fail("Loại rác không hợp lệ.");
        if (requestedItems.Any(x => x.EstimatedWeightKg < 0)) return WasteReportCreateResult.Fail("Khối lượng ước tính không được âm.");

        var categoryIds = requestedItems.Select(x => x.WasteCategoryId).Distinct().ToList();
        if (categoryIds.Count != requestedItems.Count) return WasteReportCreateResult.Fail("Không gửi trùng loại rác trong cùng một báo cáo.");

        var categories = await _wasteReportRepository.GetActiveCategoriesByIdsAsync(categoryIds, ct);
        if (categories.Count != categoryIds.Count) return WasteReportCreateResult.Fail("Một hoặc nhiều loại rác không tồn tại hoặc đã bị tắt.");

        if (request.WardId.HasValue && !await _wasteReportRepository.WardExistsAsync(request.WardId.Value, ct))
            return WasteReportCreateResult.Fail("WardId không tồn tại.");

        if (request.AreaId.HasValue && !await _wasteReportRepository.AreaExistsAsync(request.AreaId.Value, ct))
            return WasteReportCreateResult.Fail("AreaId không tồn tại.");

        if (request.Images.Count > MaxImagesPerReport)
            return WasteReportCreateResult.Fail($"Chỉ được tải tối đa {MaxImagesPerReport} ảnh.");

        var now = DateTime.UtcNow;
        var categoryById = categories.ToDictionary(x => x.Id);
        var report = new WasteReport
        {
            CitizenId = citizenId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            Description = description,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationText = string.IsNullOrWhiteSpace(request.LocationText) ? null : request.LocationText.Trim(),
            WardId = request.WardId,
            AreaId = request.AreaId,
            Status = WasteReportStatus.Pending,
            CreatedAtUtc = now,
        };

        foreach (var item in requestedItems)
        {
            var category = categoryById[item.WasteCategoryId];
            var estimatedPoints = CalculateEstimatedPoints(item.EstimatedWeightKg, category.PointsPerKg);
            report.Items.Add(new WasteReportItem
            {
                WasteCategoryId = item.WasteCategoryId,
                EstimatedWeightKg = item.EstimatedWeightKg,
                EstimatedPoints = estimatedPoints,
            });
        }

        report.EstimatedTotalPoints = report.Items.Sum(x => x.EstimatedPoints);
        report.StatusHistories.Add(new WasteReportStatusHistory
        {
            Status = WasteReportStatus.Pending,
            ChangedByUserId = citizenId,
            ChangedAtUtc = now,
            Note = "Citizen created waste report.",
        });

        try
        {
            foreach (var image in request.Images.Where(x => x.Length > 0))
            {
                var imageUrl = await SaveReportImageAsync(image, ct);
                report.Images.Add(new WasteReportImage
                {
                    ImageUrl = imageUrl,
                    OriginalFileName = Path.GetFileName(image.FileName),
                    ContentType = image.ContentType,
                    UploadedAtUtc = now,
                });
            }
        }
        catch (InvalidOperationException ex)
        {
            return WasteReportCreateResult.Fail(ex.Message);
        }

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
            Latitude = report.Latitude,
            Longitude = report.Longitude,
            LocationText = report.LocationText,
            WardId = report.WardId,
            AreaId = report.AreaId,
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
            }).ToList(),
            ImageUrls = report.Images.Select(x => x.ImageUrl).ToList(),
        };
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
