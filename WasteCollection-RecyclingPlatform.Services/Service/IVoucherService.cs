using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public interface IVoucherService
{
    // Category
    Task<List<VoucherCategoryResponse>> GetCategoriesAsync(CancellationToken ct = default);
    Task<bool> AddCategoryAsync(CategoryCreateRequest request, CancellationToken ct = default);
    Task<bool> UpdateCategoryAsync(int id, CategoryCreateRequest request, CancellationToken ct = default);
    Task<bool> DeleteCategoryAsync(int id, CancellationToken ct = default);

    // Voucher
    Task<List<VoucherResponse>> GetAllVouchersAsync(CancellationToken ct = default);
    Task<VoucherResponse?> GetVoucherByIdAsync(long id, CancellationToken ct = default);
    Task<bool> CreateVoucherAsync(VoucherCreateRequest request, CancellationToken ct = default);
    Task<bool> UpdateVoucherAsync(long id, VoucherUpdateRequest request, CancellationToken ct = default);
    Task<bool> DeleteVoucherAsync(long id, CancellationToken ct = default);

    // Redemption
    Task<(bool Success, string? VoucherCode, string? Error)> RedeemVoucherAsync(long userId, long voucherId, CancellationToken ct = default);
    Task<List<VoucherHistoryResponse>> GetRedemptionHistoryAsync(long? userId = null, CancellationToken ct = default);
}
