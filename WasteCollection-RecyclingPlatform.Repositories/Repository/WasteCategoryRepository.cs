using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Data;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public class WasteCategoryRepository : IWasteCategoryRepository
{
    private readonly AppDbContext _db;

    public WasteCategoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<WasteCategory>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.WasteCategories
            .OrderBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<WasteCategory?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await _db.WasteCategories
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<bool> ExistsByCodeAsync(string code, long? excludeId = null, CancellationToken ct = default)
    {
        return await _db.WasteCategories
            .AnyAsync(x => x.Code == code && (excludeId == null || x.Id != excludeId), ct);
    }

    public async Task AddAsync(WasteCategory category, CancellationToken ct = default)
    {
        _db.WasteCategories.Add(category);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
