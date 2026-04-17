using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Data;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public class WasteReportRepository : IWasteReportRepository
{
    private readonly AppDbContext _db;

    public WasteReportRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<WasteCategory>> GetActiveCategoriesAsync(CancellationToken ct = default)
    {
        return await _db.WasteCategories
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<WasteCategory>> GetActiveCategoriesByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default)
    {
        var categoryIds = ids.Distinct().ToList();
        return await _db.WasteCategories
            .Where(x => categoryIds.Contains(x.Id) && x.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<bool> WardExistsAsync(long wardId, CancellationToken ct = default)
    {
        return await _db.Wards.AnyAsync(x => x.Id == wardId, ct);
    }

    public async Task<bool> AreaExistsAsync(long areaId, CancellationToken ct = default)
    {
        return await _db.Areas.AnyAsync(x => x.Id == areaId, ct);
    }

    public async Task AddAsync(WasteReport report, CancellationToken ct = default)
    {
        _db.WasteReports.Add(report);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<WasteReport?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await _db.WasteReports
            .Include(x => x.Items)
                .ThenInclude(x => x.WasteCategory)
            .Include(x => x.Images)
            .Include(x => x.Ward)
            .Include(x => x.Area)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }
}
