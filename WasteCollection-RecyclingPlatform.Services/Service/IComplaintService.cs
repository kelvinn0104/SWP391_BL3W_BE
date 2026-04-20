using System.Security.Claims;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public interface IComplaintService
{
    bool TryGetCurrentUserId(ClaimsPrincipal user, out long userId);
    Task<ComplaintActionResult<ComplaintResponse>> CreateComplaintAsync(long citizenId, long reportId, ComplaintCreateRequest request, CancellationToken ct = default);
    Task<ComplaintActionResult<List<ComplaintResponse>>> GetMyComplaintsAsync(long citizenId, CancellationToken ct = default);
    Task<ComplaintActionResult<List<ComplaintResponse>>> GetComplaintsAsync(ComplaintStatus? status, CancellationToken ct = default);
    Task<ComplaintActionResult<ComplaintResponse>> GetComplaintDetailAsync(long actorUserId, bool canViewAll, long complaintId, CancellationToken ct = default);
    Task<ComplaintActionResult<ComplaintResponse>> UpdateStatusAsync(long actorUserId, long complaintId, ComplaintStatusUpdateRequest request, CancellationToken ct = default);
}
