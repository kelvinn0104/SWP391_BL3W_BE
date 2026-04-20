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

    public async Task AddAsync(WasteReport report, CancellationToken ct = default)
    {
        _db.WasteReports.Add(report);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<WasteReport?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await _db.WasteReports
            .Include(x => x.Citizen)
            .Include(x => x.AssignedCollector)
            .Include(x => x.Items)
                .ThenInclude(x => x.WasteCategory)
            .Include(x => x.Items)
                .ThenInclude(x => x.Images)
            .Include(x => x.Images)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<WasteReport?> GetByIdForAssignmentAsync(long id, CancellationToken ct = default)
    {
        return await _db.WasteReports
            .Include(x => x.Citizen)
            .Include(x => x.AssignedCollector)
            .Include(x => x.Items)
                .ThenInclude(x => x.WasteCategory)
            .Include(x => x.Items)
                .ThenInclude(x => x.Images)
            .Include(x => x.Images)
            .Include(x => x.StatusHistories)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<WasteReport?> GetByIdForUpdateAsync(long id, CancellationToken ct = default)
    {
        return await _db.WasteReports
            .Include(x => x.Items)
                .ThenInclude(x => x.WasteCategory)
            .Include(x => x.Items)
                .ThenInclude(x => x.Images)
            .Include(x => x.Images)
            .Include(x => x.StatusHistories)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<WasteReport?> GetStatusTrackingByIdAsync(long id, CancellationToken ct = default)
    {
        return await _db.WasteReports
            .Include(x => x.AssignedCollector)
            .Include(x => x.Items)
                .ThenInclude(x => x.WasteCategory)
            .Include(x => x.Items)
                .ThenInclude(x => x.Images)
            .Include(x => x.Images)
            .Include(x => x.StatusHistories)
                .ThenInclude(x => x.ChangedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<List<WasteReport>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.WasteReports
            .Include(x => x.Citizen)
            .Include(x => x.AssignedCollector)
            .Include(x => x.Items)
                .ThenInclude(x => x.WasteCategory)
            .Include(x => x.Items)
                .ThenInclude(x => x.Images)
            .Include(x => x.Images)
            .Include(x => x.StatusHistories)
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<WasteReport>> GetByCitizenIdAsync(long citizenId, CancellationToken ct = default)
    {
        return await _db.WasteReports
            .Include(x => x.Items)
                .ThenInclude(x => x.WasteCategory)
            .Include(x => x.Items)
                .ThenInclude(x => x.Images)
            .Include(x => x.Images)
            .Where(x => x.CitizenId == citizenId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<WasteReport>> GetByCitizenIdAndStatusAsync(long citizenId, WasteReportStatus status, CancellationToken ct = default)
    {
        return await _db.WasteReports
            .Include(x => x.Items)
                .ThenInclude(x => x.WasteCategory)
            .Include(x => x.Items)
                .ThenInclude(x => x.Images)
            .Include(x => x.Images)
            .Where(x => x.CitizenId == citizenId && x.Status == status)
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<WasteReport>> GetByStatusAsync(WasteReportStatus status, CancellationToken ct = default)
    {
        return await _db.WasteReports
            .Include(x => x.Items)
                .ThenInclude(x => x.WasteCategory)
            .Include(x => x.Items)
                .ThenInclude(x => x.Images)
            .Include(x => x.Images)
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<WasteReport>> GetAssignedToCollectorAsync(long collectorId, WasteReportStatus? status, CancellationToken ct = default)
    {
        var query = BuildAssignedCollectorQuery(collectorId);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        return await query
            .OrderByDescending(x => x.AssignedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<WasteReport?> GetAssignedDetailForCollectorAsync(long collectorId, long reportId, CancellationToken ct = default)
    {
        return await BuildAssignedCollectorQuery(collectorId)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == reportId, ct);
    }

    public async Task<WasteReport?> GetAssignedForCollectorUpdateAsync(long collectorId, long reportId, CancellationToken ct = default)
    {
        return await BuildAssignedCollectorQuery(collectorId)
            .Include(x => x.StatusHistories)
            .FirstOrDefaultAsync(x => x.Id == reportId, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }

    private IQueryable<WasteReport> BuildAssignedCollectorQuery(long collectorId)
    {
        return _db.WasteReports
            .Include(x => x.Citizen)
            .Include(x => x.AssignedCollector)
            .Include(x => x.Items)
                .ThenInclude(x => x.WasteCategory)
            .Include(x => x.Items)
                .ThenInclude(x => x.Images)
            .Include(x => x.Images)
            .Where(x => x.AssignedCollectorId == collectorId);
    }
}
