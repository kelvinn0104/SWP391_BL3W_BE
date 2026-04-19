using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Data;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public class ComplaintRepository : IComplaintRepository
{
    private readonly AppDbContext _db;

    public ComplaintRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Complaint complaint, CancellationToken ct = default)
    {
        _db.Complaints.Add(complaint);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Complaint?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await IncludeDetails(_db.Complaints)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<Complaint?> GetByIdForUpdateAsync(long id, CancellationToken ct = default)
    {
        return await IncludeDetails(_db.Complaints)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<List<Complaint>> GetAllAsync(ComplaintStatus? status, CancellationToken ct = default)
    {
        var query = IncludeDetails(_db.Complaints);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<Complaint>> GetByCitizenIdAsync(long citizenId, CancellationToken ct = default)
    {
        return await IncludeDetails(_db.Complaints)
            .Where(x => x.CitizenId == citizenId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsForReportAndCitizenAsync(long reportId, long citizenId, CancellationToken ct = default)
    {
        return await _db.Complaints
            .AnyAsync(x => x.WasteReportId == reportId && x.CitizenId == citizenId, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }

    private static IQueryable<Complaint> IncludeDetails(IQueryable<Complaint> query)
    {
        return query
            .Include(x => x.Citizen)
            .Include(x => x.WasteReport)
            .Include(x => x.EvidenceFiles)
            .Include(x => x.ResolvedByUser);
    }
}
