using System.Security.Claims;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public interface ICollectorJobService
{
    Task<CollectorJobListResult> GetMyJobsAsync(long collectorId, WasteReportStatus? status, CancellationToken ct = default);
    Task<CollectorJobDetailResult> GetMyJobDetailAsync(long collectorId, long reportId, CancellationToken ct = default);
    Task<CollectorJobDetailResult> AssignCollectorAsync(long actorUserId, long reportId, long collectorId, CancellationToken ct = default);
    bool TryGetCurrentUserId(ClaimsPrincipal user, out long userId);
}

