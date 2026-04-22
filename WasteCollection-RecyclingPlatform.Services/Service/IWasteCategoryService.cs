using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public interface IWasteCategoryService
{
    Task<List<WasteCategoryDetailResponse>> GetAllAsync(CancellationToken ct = default);
    Task<WasteCategoryDetailResponse?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<WasteCategoryResult> CreateAsync(WasteCategoryCreateRequest request, CancellationToken ct = default);
    Task<WasteCategoryResult> UpdatePointsAsync(long id, WasteCategoryUpdatePointsRequest request, CancellationToken ct = default);
    Task<WasteCategoryResult> ToggleActiveAsync(long id, CancellationToken ct = default);
}
