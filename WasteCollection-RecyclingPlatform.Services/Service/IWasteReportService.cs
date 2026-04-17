using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public interface IWasteReportService
{
    Task<List<WasteCategoryResponse>> GetCategoriesAsync(CancellationToken ct = default);
    Task<List<WasteReportResponse>> GetCitizenReportsAsync(long citizenId, CancellationToken ct = default);
    Task<List<WasteReportResponse>?> SearchCitizenReportsByStatusAsync(long citizenId, long statusId, CancellationToken ct = default);
    Task<WasteReportResponse?> GetCitizenReportDetailAsync(long citizenId, long reportId, CancellationToken ct = default);
    Task<WasteReportStatusTrackingResponse?> GetCitizenReportStatusAsync(long citizenId, long reportId, CancellationToken ct = default);
    Task<WasteReportCreateResult> CreateReportAsync(long citizenId, WasteReportCreateRequest request, CancellationToken ct = default);
    WasteReportFormBindResult BindWasteItemsFromRawForm(WasteReportCreateRequest request, IFormCollection? form);
    bool TryGetCurrentUserId(ClaimsPrincipal user, out long userId);
}
