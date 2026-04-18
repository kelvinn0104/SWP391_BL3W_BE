using System.Security.Claims;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Repositories.Repository;
using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public class RewardService : IRewardService
{
    private readonly IRewardRepository _rewardRepository;

    public RewardService(IRewardRepository rewardRepository)
    {
        _rewardRepository = rewardRepository;
    }

    public int CalculateEstimatedPoints(decimal? estimatedWeightKg, int pointsPerKg)
    {
        if (!estimatedWeightKg.HasValue) return 0;
        return Math.Max(0, (int)Math.Round(estimatedWeightKg.Value * pointsPerKg, MidpointRounding.AwayFromZero));
    }

    public bool TryGetCurrentUserId(ClaimsPrincipal user, out long userId)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        return long.TryParse(raw, out userId);
    }

    public async Task AwardFinalPointsForCollectedReportAsync(WasteReport report, long actorUserId, CancellationToken ct = default)
    {
        if (report.Status != WasteReportStatus.Collected)
            return;

        if (report.RewardVerifiedAtUtc.HasValue)
            return;

        var citizen = await _rewardRepository.GetUserForUpdateAsync(report.CitizenId, ct);
        if (citizen is null)
            throw new InvalidOperationException("Cannot find reward receiver.");

        var rewardPoints = Math.Max(0, report.EstimatedTotalPoints);
        citizen.Points += rewardPoints;

        var now = DateTime.UtcNow;
        report.FinalRewardPoints = rewardPoints;
        report.RewardVerifiedAtUtc = now;

        _rewardRepository.AddRewardPointTransaction(new RewardPointTransaction
        {
            UserId = citizen.Id,
            Amount = rewardPoints,
            BalanceAfter = citizen.Points,
            TransactionType = RewardPointTransactionType.Earned,
            SourceType = RewardPointSourceType.WasteReportCollected,
            SourceRefId = report.Id,
            Description = $"Reward points for collected report #{report.Id}",
            CreatedByUserId = actorUserId,
            CreatedAtUtc = now,
        });
    }

    public async Task<RewardActionResult<RewardPointHistoryResponse>> GetPointHistoryAsync(long userId, int skip, int take, CancellationToken ct = default)
    {
        try
        {
            var safeSkip = Math.Max(0, skip);
            var safeTake = Math.Clamp(take, 1, 200);

            var user = await _rewardRepository.GetUserByIdAsync(userId, ct);
            if (user is null)
                return RewardActionResult<RewardPointHistoryResponse>.UnauthorizedResult("User does not exist.");

            var page = await _rewardRepository.GetPointTransactionsAsync(userId, safeSkip, safeTake, ct);

            return RewardActionResult<RewardPointHistoryResponse>.Ok(new RewardPointHistoryResponse
            {
                CurrentBalance = user.Points,
                TotalTransactions = page.TotalTransactions,
                Transactions = page.Transactions.Select(MapPointTransaction).ToList(),
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return RewardActionResult<RewardPointHistoryResponse>.UnauthorizedResult(ex.Message);
        }
    }

    public async Task<RewardActionResult<PointBalanceResponse>> GetPointBalanceAsync(long userId, CancellationToken ct = default)
    {
        try
        {
            var user = await _rewardRepository.GetUserByIdAsync(userId, ct);
            if (user is null)
                return RewardActionResult<PointBalanceResponse>.UnauthorizedResult("User does not exist.");

            return RewardActionResult<PointBalanceResponse>.Ok(new PointBalanceResponse
            {
                CurrentBalance = user.Points,
                Points = user.Points,
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return RewardActionResult<PointBalanceResponse>.UnauthorizedResult(ex.Message);
        }
    }

    public async Task<RewardActionResult<UserLeaderboardResponse>> GetUserLeaderboardAsync(long currentUserId, int skip, int take, CancellationToken ct = default)
    {
        var safeSkip = Math.Max(0, skip);
        var safeTake = Math.Clamp(take, 1, 200);

        var users = await _rewardRepository.GetCitizenLeaderboardRowsAsync(ct);
        var myRank = users.FindIndex(x => x.UserId == currentUserId);
        var myPoints = users.FirstOrDefault(x => x.UserId == currentUserId)?.Points;

        return RewardActionResult<UserLeaderboardResponse>.Ok(new UserLeaderboardResponse
        {
            TotalParticipants = users.Count,
            MyRank = myRank >= 0 ? myRank + 1 : null,
            MyPoints = myPoints,
            Users = users
                .Skip(safeSkip)
                .Take(safeTake)
                .Select((x, idx) => MapUserLeaderboardItem(
                    rank: safeSkip + idx + 1,
                    userId: x.UserId,
                    displayName: x.DisplayName,
                    avatarUrl: x.AvatarUrl,
                    points: x.Points,
                    completedReports: x.CompletedReports))
                .ToList(),
        });
    }

    public async Task<List<AreaLeaderboardItemResponse>> GetAreaLeaderboardAsync(int skip, int take, CancellationToken ct = default)
    {
        var safeSkip = Math.Max(0, skip);
        var safeTake = Math.Clamp(take, 1, 200);

        var areaUserRows = await _rewardRepository.GetAreaUserPointRowsAsync(ct);
        var ranked = areaUserRows
            .GroupBy(x => new { x.AreaId, x.AreaName })
            .Select(g => new
            {
                g.Key.AreaId,
                g.Key.AreaName,
                TotalPoints = g.Sum(x => x.Points),
                ParticipantCount = g.Select(x => x.UserId).Distinct().Count(),
            })
            .OrderByDescending(x => x.TotalPoints)
            .ThenByDescending(x => x.ParticipantCount)
            .ThenBy(x => x.AreaName)
            .ToList();

        return ranked
            .Skip(safeSkip)
            .Take(safeTake)
            .Select((x, idx) => new AreaLeaderboardItemResponse
            {
                Rank = safeSkip + idx + 1,
                AreaId = x.AreaId,
                AreaName = x.AreaName,
                TotalPoints = x.TotalPoints,
                ParticipantCount = x.ParticipantCount,
            })
            .ToList();
    }

    public async Task<RewardActionResult<AreaUserLeaderboardResponse>> GetAreaUserLeaderboardAsync(long areaId, long currentUserId, int skip, int take, CancellationToken ct = default)
    {
        var safeSkip = Math.Max(0, skip);
        var safeTake = Math.Clamp(take, 1, 200);

        var area = await _rewardRepository.GetAreaByIdAsync(areaId, ct);
        if (area is null)
            return RewardActionResult<AreaUserLeaderboardResponse>.NotFoundResult("Area not found.");

        var usersInArea = await _rewardRepository.GetCitizenLeaderboardRowsByAreaAsync(areaId, ct);
        var myRank = usersInArea.FindIndex(x => x.UserId == currentUserId);
        var myPoints = usersInArea.FirstOrDefault(x => x.UserId == currentUserId)?.Points;

        return RewardActionResult<AreaUserLeaderboardResponse>.Ok(new AreaUserLeaderboardResponse
        {
            AreaId = area.Id,
            AreaName = area.DistrictName,
            TotalParticipants = usersInArea.Count,
            MyRank = myRank >= 0 ? myRank + 1 : null,
            MyPoints = myPoints,
            Users = usersInArea
                .Skip(safeSkip)
                .Take(safeTake)
                .Select((x, idx) => MapUserLeaderboardItem(
                    rank: safeSkip + idx + 1,
                    userId: x.UserId,
                    displayName: x.DisplayName,
                    avatarUrl: x.AvatarUrl,
                    points: x.Points,
                    completedReports: x.CompletedReports))
                .ToList(),
        });
    }

    private static RewardPointTransactionItemResponse MapPointTransaction(RewardPointTransaction transaction)
    {
        return new RewardPointTransactionItemResponse
        {
            Id = transaction.Id,
            Amount = transaction.Amount,
            BalanceAfter = transaction.BalanceAfter,
            TransactionType = transaction.TransactionType.ToString(),
            SourceType = transaction.SourceType.ToString(),
            SourceRefId = transaction.SourceRefId,
            Description = transaction.Description,
            CreatedAtUtc = transaction.CreatedAtUtc,
        };
    }

    private static AreaUserLeaderboardItemResponse MapUserLeaderboardItem(
        int rank,
        long userId,
        string displayName,
        string? avatarUrl,
        int points,
        int completedReports)
    {
        return new AreaUserLeaderboardItemResponse
        {
            Rank = rank,
            UserId = userId,
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
            Points = points,
            CompletedReports = completedReports,
            Name = displayName,
            Avatar = avatarUrl,
            Actions = completedReports,
        };
    }
}
