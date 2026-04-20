using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Data;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public class PasswordResetRepository : IPasswordResetRepository
{
    private readonly AppDbContext _db;

    public PasswordResetRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(PasswordReset reset, CancellationToken ct = default)
    {
        _db.PasswordResets.Add(reset);
        await _db.SaveChangesAsync(ct);
    }

    // Lấy bản ghi reset mới nhất còn hiệu lực (chưa dùng, chưa hết hạn)
    public async Task<PasswordReset?> GetLatestActiveAsync(long userId, DateTime now, CancellationToken ct = default)
        => await _db.PasswordResets
            .Where(x => x.UserId == userId && x.UsedAtUtc == null && x.ExpiresAtUtc > now)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

    // Lấy bản ghi đã verify code thành công, còn token chưa hết hạn
    public async Task<PasswordReset?> GetLatestVerifiedAsync(long userId, DateTime now, CancellationToken ct = default)
        => await _db.PasswordResets
            .Where(x => x.UserId == userId
                     && x.UsedAtUtc == null
                     && x.ResetTokenExpiresAtUtc != null
                     && x.ResetTokenExpiresAtUtc > now)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

    public async Task UpdateAsync(PasswordReset reset, CancellationToken ct = default)
    {
        _db.PasswordResets.Update(reset);
        await _db.SaveChangesAsync(ct);
    }
}
