using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Data;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public class VoucherRepository : IVoucherRepository
{
    private readonly AppDbContext _db;

    public VoucherRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<VoucherCategory>> GetCategoriesAsync(CancellationToken ct = default)
    {
        return await _db.VoucherCategories
            .Include(c => c.Vouchers)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<VoucherCategory?> GetCategoryByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.VoucherCategories.FindAsync(new object[] { id }, ct);
    }

    public async Task AddCategoryAsync(VoucherCategory category, CancellationToken ct = default)
    {
        _db.VoucherCategories.Add(category);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateCategoryAsync(VoucherCategory category, CancellationToken ct = default)
    {
        _db.VoucherCategories.Update(category);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteCategoryAsync(VoucherCategory category, CancellationToken ct = default)
    {
        _db.VoucherCategories.Remove(category);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<Voucher>> GetVouchersAsync(CancellationToken ct = default)
    {
        return await _db.Vouchers
            .Include(v => v.Category)
            .Include(v => v.Codes)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Voucher?> GetVoucherByIdAsync(long id, CancellationToken ct = default)
    {
        return await _db.Vouchers
            .Include(v => v.Category)
            .Include(v => v.Codes)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task AddVoucherAsync(Voucher voucher, CancellationToken ct = default)
    {
        _db.Vouchers.Add(voucher);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateVoucherAsync(Voucher voucher, CancellationToken ct = default)
    {
        _db.Vouchers.Update(voucher);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteVoucherAsync(Voucher voucher, CancellationToken ct = default)
    {
        _db.Vouchers.Remove(voucher);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<VoucherCode?> GetNextAvailableCodeAsync(long voucherId, CancellationToken ct = default)
    {
        return await _db.VoucherCodes
            .Where(c => c.VoucherId == voucherId && !c.IsUsed)
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task UpdateVoucherCodeAsync(VoucherCode code, CancellationToken ct = default)
    {
        _db.VoucherCodes.Update(code);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<VoucherCode>> GetRedemptionHistoryAsync(long? userId = null, CancellationToken ct = default)
    {
        var query = _db.VoucherCodes
            .Include(c => c.Voucher)
            .Include(c => c.UsedByUser)
            .Where(c => c.IsUsed);

        if (userId.HasValue)
        {
            query = query.Where(c => c.UsedByUserId == userId.Value);
        }

        return await query
            .OrderByDescending(c => c.UsedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
