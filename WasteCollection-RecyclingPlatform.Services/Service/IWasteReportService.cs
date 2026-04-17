using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public interface IWasteReportService
{
    Task<List<WasteCategoryResponse>> GetCategoriesAsync(CancellationToken ct = default);
    Task<WasteReportCreateResult> CreateReportAsync(long citizenId, WasteReportCreateRequest request, CancellationToken ct = default);
}
