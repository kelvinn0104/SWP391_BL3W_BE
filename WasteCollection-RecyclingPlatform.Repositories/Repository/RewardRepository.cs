using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Data;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public class RewardRepository : IRewardRepository
{
    private readonly AppDbContext _db;

    public RewardRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetUserByIdAsync(long userId, CancellationToken ct = default)
    {
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, ct);
    }

    public async Task<User?> GetUserForUpdateAsync(long userId, CancellationToken ct = default)
    {
        return await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
    }

    public async Task<RewardPointTransactionPage> GetPointTransactionsAsync(long userId, int skip, int take, CancellationToken ct = default)
    {
        var query = _db.RewardPointTransactions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id);

        return new RewardPointTransactionPage
        {
            TotalTransactions = await query.CountAsync(ct),
            Transactions = await query.Skip(skip).Take(take).ToListAsync(ct),
        };
    }

    public async Task<List<UserLeaderboardRow>> GetCitizenLeaderboardRowsAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Citizen)
            .Select(u => new UserLeaderboardRow
            {
                UserId = u.Id,
                DisplayName = u.DisplayName ?? u.FullName ?? u.Email,
                AvatarUrl = u.AvatarUrl,
                Points = u.Points,
                CompletedReports = _db.WasteReports.Count(r => r.CitizenId == u.Id && r.Status == WasteReportStatus.Collected),
            })
            .OrderByDescending(x => x.Points)
            .ThenByDescending(x => x.CompletedReports)
            .ThenBy(x => x.DisplayName)
            .ToListAsync(ct);
    }

    public async Task<List<AreaUserPointRow>> GetAreaUserPointRowsAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Citizen)
            .SelectMany(
                u => u.Wards.Select(w => new
                {
                    w.AreaId,
                    AreaName = w.Area.DistrictName,
                    UserId = u.Id,
                    u.Points,
                }))
            .GroupBy(x => new { x.AreaId, x.AreaName, x.UserId })
            .Select(g => new AreaUserPointRow
            {
                AreaId = g.Key.AreaId,
                AreaName = g.Key.AreaName,
                UserId = g.Key.UserId,
                Points = g.Max(x => x.Points),
            })
            .ToListAsync(ct);
    }

    public async Task<Area?> GetAreaByIdAsync(long areaId, CancellationToken ct = default)
    {
        return await _db.Areas.Include(a => a.Wards).FirstOrDefaultAsync(x => x.Id == areaId, ct);
    }

    public async Task<Ward?> GetWardByIdAsync(long wardId, CancellationToken ct = default)
    {
        return await _db.Wards.Include(w => w.Area).FirstOrDefaultAsync(x => x.Id == wardId, ct);
    }

    public async Task<List<UserLeaderboardRow>> GetCitizenLeaderboardRowsByAreaAsync(long areaId, CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Citizen && u.Wards.Any(w => w.AreaId == areaId))
            .Select(u => new UserLeaderboardRow
            {
                UserId = u.Id,
                DisplayName = u.DisplayName ?? u.FullName ?? u.Email,
                AvatarUrl = u.AvatarUrl,
                Points = u.Points,
                CompletedReports = _db.WasteReports.Count(r => r.CitizenId == u.Id && r.Status == WasteReportStatus.Collected),
            })
            .OrderByDescending(x => x.Points)
            .ThenByDescending(x => x.CompletedReports)
            .ThenBy(x => x.DisplayName)
            .ToListAsync(ct);
    }

    public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        return await _db.Database.BeginTransactionAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }

    public void AddRewardPointTransaction(RewardPointTransaction transaction)
    {
        _db.RewardPointTransactions.Add(transaction);
    }
}
