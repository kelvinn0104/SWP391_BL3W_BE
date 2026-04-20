using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public interface IWasteReportRepository
{
    Task<List<WasteCategory>> GetActiveCategoriesAsync(CancellationToken ct = default);
    Task<List<WasteCategory>> GetActiveCategoriesByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
    Task AddAsync(WasteReport report, CancellationToken ct = default);
    Task<WasteReport?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<WasteReport?> GetByIdForUpdateAsync(long id, CancellationToken ct = default);
    Task<WasteReport?> GetByIdForAssignmentAsync(long id, CancellationToken ct = default);
    Task<WasteReport?> GetStatusTrackingByIdAsync(long id, CancellationToken ct = default);
    Task<List<WasteReport>> GetAllAsync(CancellationToken ct = default);
    Task<List<WasteReport>> GetByCitizenIdAsync(long citizenId, CancellationToken ct = default);
    Task<List<WasteReport>> GetByCitizenIdAndStatusAsync(long citizenId, WasteReportStatus status, CancellationToken ct = default);
    Task<List<WasteReport>> GetByStatusAsync(WasteReportStatus status, CancellationToken ct = default);
    Task<List<WasteReport>> GetAssignedToCollectorAsync(long collectorId, WasteReportStatus? status, CancellationToken ct = default);
    Task<WasteReport?> GetAssignedDetailForCollectorAsync(long collectorId, long reportId, CancellationToken ct = default);
    Task<WasteReport?> GetAssignedForCollectorUpdateAsync(long collectorId, long reportId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
