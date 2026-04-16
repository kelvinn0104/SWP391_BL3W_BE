using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public interface ICollectionRequestRepository
{
    Task<List<CollectionRequest>> GetAllAsync(CancellationToken ct = default);
    Task<CollectionRequest?> GetByIdAsync(long id, CancellationToken ct = default);
    Task AddAsync(CollectionRequest request, CancellationToken ct = default);
    Task UpdateAsync(CollectionRequest request, CancellationToken ct = default);
    Task<List<CollectionRequest>> GetByCitizenIdAsync(long citizenId, CancellationToken ct = default);
    Task<List<CollectionRequest>> GetByCollectorIdAsync(long collectorId, CancellationToken ct = default);
}
