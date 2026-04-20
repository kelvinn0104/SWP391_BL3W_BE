using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Data;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public class CollectionRequestRepository : ICollectionRequestRepository
{
    private readonly AppDbContext _db;

    public CollectionRequestRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CollectionRequest>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.CollectionRequests
            .Include(x => x.Citizen)
            .Include(x => x.Collector)
            .Include(x => x.Ward)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<CollectionRequest?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await _db.CollectionRequests
            .Include(x => x.Citizen)
            .Include(x => x.Collector)
            .Include(x => x.Ward)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task AddAsync(CollectionRequest request, CancellationToken ct = default)
    {
        _db.CollectionRequests.Add(request);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CollectionRequest request, CancellationToken ct = default)
    {
        _db.CollectionRequests.Update(request);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<CollectionRequest>> GetByCitizenIdAsync(long citizenId, CancellationToken ct = default)
    {
        return await _db.CollectionRequests
            .Include(x => x.Collector)
            .Include(x => x.Ward)
            .Where(x => x.CitizenId == citizenId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<List<CollectionRequest>> GetByCollectorIdAsync(long collectorId, CancellationToken ct = default)
    {
        return await _db.CollectionRequests
            .Include(x => x.Citizen)
            .Include(x => x.Ward)
            .Where(x => x.CollectorId == collectorId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }
}
