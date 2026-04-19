using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public interface IComplaintRepository
{
    Task AddAsync(Complaint complaint, CancellationToken ct = default);
    Task<Complaint?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Complaint?> GetByIdForUpdateAsync(long id, CancellationToken ct = default);
    Task<List<Complaint>> GetAllAsync(ComplaintStatus? status, CancellationToken ct = default);
    Task<List<Complaint>> GetByCitizenIdAsync(long citizenId, CancellationToken ct = default);
    Task<bool> ExistsForReportAndCitizenAsync(long reportId, long citizenId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
