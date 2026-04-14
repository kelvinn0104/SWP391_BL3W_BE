using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public interface IAreaService
{
    Task<List<AreaResponse>> GetAllAreasAsync(CancellationToken ct = default);
    Task<List<AreaResponse>> BulkUpdateAreasAsync(List<AreaResponse> areas, CancellationToken ct = default);
    Task<bool> DeleteAreaAsync(string id, CancellationToken ct = default);
    Task<bool> UpdateAreaInfoAsync(string id, AreaResponse dto, CancellationToken ct = default);
}
