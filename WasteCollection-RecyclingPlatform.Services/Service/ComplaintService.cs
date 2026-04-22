using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Repositories.Repository;
using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public class ComplaintService : IComplaintService
{
    private const int MaxEvidenceFiles = 5;
    private const long MaxEvidenceBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedEvidenceExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".pdf",
    };

    private readonly IComplaintRepository _complaintRepository;
    private readonly IWasteReportRepository _wasteReportRepository;
    private readonly IUserRepository _userRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INotificationService _notificationService;

    public ComplaintService(
        IComplaintRepository complaintRepository,
        IWasteReportRepository wasteReportRepository,
        IUserRepository userRepository,
        IHttpContextAccessor httpContextAccessor,
        INotificationService notificationService)
    {
        _complaintRepository = complaintRepository;
        _wasteReportRepository = wasteReportRepository;
        _userRepository = userRepository;
        _httpContextAccessor = httpContextAccessor;
        _notificationService = notificationService;
    }

    public bool TryGetCurrentUserId(ClaimsPrincipal user, out long userId)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst("id")?.Value;

        return long.TryParse(raw, out userId);
    }

    public async Task<ComplaintActionResult<ComplaintResponse>> CreateComplaintAsync(long citizenId, long reportId, ComplaintCreateRequest request, CancellationToken ct = default)
    {
        try
        {
            var citizen = await _userRepository.GetByIdAsync(citizenId, ct);
            if (citizen is null)
                return ComplaintActionResult<ComplaintResponse>.UnauthorizedResult("Người dùng không tồn tại.");

            if (citizen.Role != UserRole.Citizen)
                return ComplaintActionResult<ComplaintResponse>.Fail("Chỉ công dân mới được tạo phản ánh.");

            var report = await _wasteReportRepository.GetByIdAsync(reportId, ct);
            if (report is null || report.CitizenId != citizenId)
                return ComplaintActionResult<ComplaintResponse>.NotFoundResult("Không tìm thấy báo cáo rác.");

            if (report.Status is not (WasteReportStatus.Collected or WasteReportStatus.Cancelled))
                return ComplaintActionResult<ComplaintResponse>.Fail("Chỉ có thể phản ánh báo cáo đã thu gom hoặc đã hủy.");

            var reason = request.Reason?.Trim();
            if (string.IsNullOrWhiteSpace(reason))
                return ComplaintActionResult<ComplaintResponse>.Fail("Lý do phản ánh là bắt buộc.");

            var description = request.Description?.Trim();
            if (string.IsNullOrWhiteSpace(description))
                return ComplaintActionResult<ComplaintResponse>.Fail("Mô tả phản ánh là bắt buộc.");

            if (await _complaintRepository.ExistsForReportAndCitizenAsync(reportId, citizenId, ct))
                return ComplaintActionResult<ComplaintResponse>.Fail("Báo cáo này đã có phản ánh.");

            var evidenceFiles = request.EvidenceFiles.Where(x => x.Length > 0).ToList();
            if (evidenceFiles.Count > MaxEvidenceFiles)
                return ComplaintActionResult<ComplaintResponse>.Fail($"Chỉ được tải tối đa {MaxEvidenceFiles} tệp bằng chứng.");

            var now = DateTime.UtcNow;
            var complaint = new Complaint
            {
                WasteReportId = reportId,
                CitizenId = citizenId,
                Reason = reason,
                Description = description,
                Status = ComplaintStatus.Submitted,
                CreatedAtUtc = now,
            };

            foreach (var file in evidenceFiles)
            {
                var fileUrl = await SaveEvidenceAsync(file, ct);
                complaint.EvidenceFiles.Add(new ComplaintEvidence
                {
                    FileUrl = fileUrl,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    ContentType = file.ContentType,
                    UploadedAtUtc = now,
                });
            }

            await _complaintRepository.AddAsync(complaint, ct);

            var enterprises = await _userRepository.GetByRoleAsync(UserRole.RecyclingEnterprise, null, ct);
            var admins = await _userRepository.GetByRoleAsync(UserRole.Administrator, null, ct);
            var notifyIds = enterprises.Select(x => x.Id).Concat(admins.Select(x => x.Id)).Distinct().ToList();

            if (notifyIds.Any())
            {
                await _notificationService.NotifyComplaintSubmittedAsync(complaint.Id, reportId, citizen.DisplayName ?? citizen.FullName ?? citizen.Email, notifyIds, ct);
            }

            var saved = await _complaintRepository.GetByIdAsync(complaint.Id, ct);
            return ComplaintActionResult<ComplaintResponse>.Ok(MapComplaint(saved ?? complaint));
        }
        catch (InvalidOperationException ex)
        {
            return ComplaintActionResult<ComplaintResponse>.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return ComplaintActionResult<ComplaintResponse>.Fail($"Không thể tạo phản ánh: {ex.Message}");
        }
    }

    public async Task<ComplaintActionResult<List<ComplaintResponse>>> GetMyComplaintsAsync(long citizenId, CancellationToken ct = default)
    {
        try
        {
            var complaints = await _complaintRepository.GetByCitizenIdAsync(citizenId, ct);
            return ComplaintActionResult<List<ComplaintResponse>>.Ok(complaints.Select(MapComplaint).ToList());
        }
        catch (Exception ex)
        {
            return ComplaintActionResult<List<ComplaintResponse>>.Fail($"Không thể tải danh sách phản ánh: {ex.Message}");
        }
    }

    public async Task<ComplaintActionResult<List<ComplaintResponse>>> GetComplaintsAsync(ComplaintStatus? status, CancellationToken ct = default)
    {
        try
        {
            if (status.HasValue && !Enum.IsDefined(status.Value))
                return ComplaintActionResult<List<ComplaintResponse>>.Fail("Trạng thái phản ánh không hợp lệ.");

            var complaints = await _complaintRepository.GetAllAsync(status, ct);
            return ComplaintActionResult<List<ComplaintResponse>>.Ok(complaints.Select(MapComplaint).ToList());
        }
        catch (Exception ex)
        {
            return ComplaintActionResult<List<ComplaintResponse>>.Fail($"Không thể tải danh sách phản ánh: {ex.Message}");
        }
    }

    public async Task<ComplaintActionResult<ComplaintResponse>> GetComplaintDetailAsync(long actorUserId, bool canViewAll, long complaintId, CancellationToken ct = default)
    {
        try
        {
            var complaint = await _complaintRepository.GetByIdAsync(complaintId, ct);
            if (complaint is null)
                return ComplaintActionResult<ComplaintResponse>.NotFoundResult("Không tìm thấy phản ánh.");

            if (!canViewAll && complaint.CitizenId != actorUserId)
                return ComplaintActionResult<ComplaintResponse>.NotFoundResult("Không tìm thấy phản ánh.");

            return ComplaintActionResult<ComplaintResponse>.Ok(MapComplaint(complaint));
        }
        catch (Exception ex)
        {
            return ComplaintActionResult<ComplaintResponse>.Fail($"Không thể tải phản ánh: {ex.Message}");
        }
    }

    public async Task<ComplaintActionResult<ComplaintResponse>> UpdateStatusAsync(long actorUserId, long complaintId, ComplaintStatusUpdateRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!Enum.IsDefined(request.Status))
                return ComplaintActionResult<ComplaintResponse>.Fail("Trạng thái phản ánh không hợp lệ. Các giá trị hợp lệ: Submitted, InReview, Resolved, Rejected.");

            var complaint = await _complaintRepository.GetByIdForUpdateAsync(complaintId, ct);
            if (complaint is null)
                return ComplaintActionResult<ComplaintResponse>.NotFoundResult("Không tìm thấy phản ánh.");

            var note = request.AdminNote?.Trim();
            var now = DateTime.UtcNow;
            complaint.Status = request.Status;
            complaint.AdminNote = string.IsNullOrWhiteSpace(note) ? complaint.AdminNote : note;
            complaint.UpdatedAtUtc = now;

            if (request.Status is ComplaintStatus.Resolved or ComplaintStatus.Rejected)
            {
                complaint.ResolvedByUserId = actorUserId;
                complaint.ResolvedAtUtc = now;
            }
            else
            {
                complaint.ResolvedByUserId = null;
                complaint.ResolvedAtUtc = null;
            }

            await _complaintRepository.SaveChangesAsync(ct);

            await _notificationService.NotifyComplaintStatusUpdatedAsync(complaint.Id, complaint.WasteReportId, complaint.CitizenId, request.Status.ToString(), note, ct);

            var saved = await _complaintRepository.GetByIdAsync(complaint.Id, ct);
            return ComplaintActionResult<ComplaintResponse>.Ok(MapComplaint(saved ?? complaint));
        }
        catch (Exception ex)
        {
            return ComplaintActionResult<ComplaintResponse>.Fail($"Không thể cập nhật trạng thái phản ánh: {ex.Message}");
        }
    }

    private ComplaintResponse MapComplaint(Complaint complaint)
    {
        var sender = complaint.Citizen?.DisplayName ?? complaint.Citizen?.FullName ?? string.Empty;
        var email = complaint.Citizen?.Email ?? string.Empty;

        return new ComplaintResponse
        {
            Id = complaint.Id,
            WasteReportId = complaint.WasteReportId,
            CitizenId = complaint.CitizenId,
            CitizenName = sender,
            CitizenEmail = email,
            ReportTitle = complaint.WasteReport?.Title,
            Reason = complaint.Reason,
            Description = complaint.Description,
            Status = complaint.Status.ToString(),
            AdminNote = complaint.AdminNote,
            ResolvedByUserId = complaint.ResolvedByUserId,
            ResolvedByName = complaint.ResolvedByUser?.DisplayName ?? complaint.ResolvedByUser?.FullName,
            ResolvedAtUtc = complaint.ResolvedAtUtc,
            CreatedAtUtc = complaint.CreatedAtUtc,
            UpdatedAtUtc = complaint.UpdatedAtUtc,
            EvidenceFiles = complaint.EvidenceFiles.Select(x => new ComplaintEvidenceResponse
            {
                Id = x.Id,
                FileUrl = ToClientFileUrl(x.FileUrl),
                OriginalFileName = x.OriginalFileName,
                ContentType = x.ContentType,
                UploadedAtUtc = x.UploadedAtUtc,
            }).ToList(),
        };
    }

    private string ToClientFileUrl(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return fileUrl;

        if (Uri.TryCreate(fileUrl, UriKind.Absolute, out _))
            return fileUrl;

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is null)
            return fileUrl;

        var filePath = fileUrl.StartsWith("/", StringComparison.Ordinal)
            ? fileUrl
            : $"/{fileUrl}";

        return $"{request.Scheme}://{request.Host}{request.PathBase}{filePath}";
    }

    private static async Task<string> SaveEvidenceAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length > MaxEvidenceBytes)
            throw new InvalidOperationException("Tệp bằng chứng không được vượt quá 10MB.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedEvidenceExtensions.Contains(extension))
            throw new InvalidOperationException("Định dạng tệp bằng chứng không được hỗ trợ.");

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chỉ hỗ trợ tệp bằng chứng dạng hình ảnh hoặc PDF.");
        }

        var uploadDirectory = ResolveUploadDirectory();
        Directory.CreateDirectory(uploadDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(uploadDirectory, fileName);

        await using var stream = new FileStream(filePath, FileMode.CreateNew);
        await file.CopyToAsync(stream, ct);

        return $"/complaint-evidence/{fileName}";
    }

    private static string ResolveUploadDirectory()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "wwwroot", "complaint-evidence"));
    }
}
