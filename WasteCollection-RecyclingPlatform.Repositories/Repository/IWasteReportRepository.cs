using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public interface IWasteReportRepository
{
    Task<List<WasteCategory>> GetActiveCategoriesAsync(CancellationToken ct = default);
    Task<List<WasteCategory>> GetActiveCategoriesByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
    Task<bool> WardExistsAsync(long wardId, CancellationToken ct = default);
    Task<bool> AreaExistsAsync(long areaId, CancellationToken ct = default);
    Task AddAsync(WasteReport report, CancellationToken ct = default);
    Task<WasteReport?> GetByIdAsync(long id, CancellationToken ct = default);
}
