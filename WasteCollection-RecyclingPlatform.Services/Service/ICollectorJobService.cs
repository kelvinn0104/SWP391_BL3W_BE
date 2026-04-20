using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public interface ICollectorJobService
{
    Task<CollectorJobListResult> GetMyJobsAsync(long collectorId, WasteReportStatus? status, CancellationToken ct = default);
    Task<CollectorJobDetailResult> GetMyJobDetailAsync(long collectorId, long reportId, CancellationToken ct = default);
    Task<CollectorJobDetailResult> AssignCollectorAsync(long actorUserId, long reportId, long collectorId, CancellationToken ct = default);
    Task<CollectorJobDetailResult> AcceptMyJobAsync(long collectorId, long reportId, string? note, CancellationToken ct = default);
    Task<CollectorJobDetailResult> CancelMyJobAsync(long collectorId, long reportId, string? note, CancellationToken ct = default);
    Task<CollectorJobCompletionResult> CompleteMyJobAsync(long collectorId, long reportId, CollectorJobCompletionRequest request, CancellationToken ct = default);
    CollectorJobFormBindResult BindCompletionRequestFromRawForm(CollectorJobCompletionRequest request, IFormCollection? form);
    bool TryGetCurrentUserId(ClaimsPrincipal user, out long userId);
}
