using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public interface IVoucherRepository
{
    // Voucher Category
    Task<List<VoucherCategory>> GetCategoriesAsync(CancellationToken ct = default);
    Task<VoucherCategory?> GetCategoryByIdAsync(int id, CancellationToken ct = default);
    Task AddCategoryAsync(VoucherCategory category, CancellationToken ct = default);
    Task UpdateCategoryAsync(VoucherCategory category, CancellationToken ct = default);
    Task DeleteCategoryAsync(VoucherCategory category, CancellationToken ct = default);

    // Voucher
    Task<List<Voucher>> GetVouchersAsync(CancellationToken ct = default);
    Task<Voucher?> GetVoucherByIdAsync(long id, CancellationToken ct = default);
    Task AddVoucherAsync(Voucher voucher, CancellationToken ct = default);
    Task UpdateVoucherAsync(Voucher voucher, CancellationToken ct = default);
    Task DeleteVoucherAsync(Voucher voucher, CancellationToken ct = default);

    // Voucher Code
    Task<VoucherCode?> GetNextAvailableCodeAsync(long voucherId, CancellationToken ct = default);
    Task UpdateVoucherCodeAsync(VoucherCode code, CancellationToken ct = default);
    Task<List<VoucherCode>> GetRedemptionHistoryAsync(long? userId = null, CancellationToken ct = default);
}
