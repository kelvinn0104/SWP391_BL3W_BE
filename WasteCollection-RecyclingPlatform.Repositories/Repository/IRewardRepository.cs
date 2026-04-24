using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public class RewardPointTransactionPage
{
    public int TotalTransactions { get; set; }
    public List<RewardPointTransaction> Transactions { get; set; } = new();
}

public class UserLeaderboardRow
{
    public long UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public int Points { get; set; }
    public int CompletedReports { get; set; }
}

public class AreaUserPointRow
{
    public long AreaId { get; set; }
    public string AreaName { get; set; } = null!;
    public long UserId { get; set; }
    public int Points { get; set; }
}

public interface IRewardRepository
{
    Task<User?> GetUserByIdAsync(long userId, CancellationToken ct = default);
    Task<User?> GetUserForUpdateAsync(long userId, CancellationToken ct = default);
    Task<RewardPointTransactionPage> GetPointTransactionsAsync(long userId, int skip, int take, CancellationToken ct = default);
    public Task<List<UserLeaderboardRow>> GetCitizenLeaderboardRowsAsync(CancellationToken ct = default);
    Task<List<AreaUserPointRow>> GetAreaUserPointRowsAsync(CancellationToken ct = default);
    Task<Area?> GetAreaByIdAsync(long areaId, CancellationToken ct = default);
    Task<Ward?> GetWardByIdAsync(long wardId, CancellationToken ct = default);
    Task<List<UserLeaderboardRow>> GetCitizenLeaderboardRowsByAreaAsync(long areaId, CancellationToken ct = default);
    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    void AddRewardPointTransaction(RewardPointTransaction transaction);
}
