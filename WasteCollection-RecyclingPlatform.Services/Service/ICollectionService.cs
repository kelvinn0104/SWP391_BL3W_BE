using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Services.Model;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public interface ICollectionService
{
    Task<List<CollectionRequestResponse>> GetAllRequestsAsync(CancellationToken ct = default);
    Task<CollectionRequestResponse?> GetRequestByIdAsync(long id, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(long id, CollectionRequestStatus status, string? reason = null, CancellationToken ct = default);
    Task<bool> AssignCollectorAsync(long id, long collectorId, CancellationToken ct = default);
    Task<List<CollectionRequestResponse>> GetCitizenRequestsAsync(long citizenId, CancellationToken ct = default);
    Task<List<CollectionRequestResponse>> GetCollectorRequestsAsync(long collectorId, CancellationToken ct = default);
}
