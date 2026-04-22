using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public interface IWasteCategoryRepository
{
    Task<List<WasteCategory>> GetAllAsync(CancellationToken ct = default);
    Task<WasteCategory?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, long? excludeId = null, CancellationToken ct = default);
    Task AddAsync(WasteCategory category, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
