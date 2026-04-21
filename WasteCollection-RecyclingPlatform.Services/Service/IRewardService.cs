using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Services.DTOs;
using System.Security.Claims;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public interface IRewardService
{
    int CalculateEstimatedPoints(decimal? estimatedWeightKg, int pointsPerKg);
    bool TryGetCurrentUserId(ClaimsPrincipal user, out long userId);
    Task AwardFinalPointsForCollectedReportAsync(WasteReport report, long actorUserId, CancellationToken ct = default);
    Task<RewardActionResult<RewardPointHistoryResponse>> GetPointHistoryAsync(long userId, int skip, int take, CancellationToken ct = default);
    Task<RewardActionResult<PointBalanceResponse>> GetPointBalanceAsync(long userId, CancellationToken ct = default);
    Task<RewardActionResult<UserLeaderboardResponse>> GetUserLeaderboardAsync(long currentUserId, int skip, int take, CancellationToken ct = default);
    Task<List<AreaLeaderboardItemResponse>> GetAreaLeaderboardAsync(int skip, int take, CancellationToken ct = default);
    Task<RewardActionResult<AreaUserLeaderboardResponse>> GetAreaUserLeaderboardAsync(long areaId, long currentUserId, int skip, int take, CancellationToken ct = default);
}
