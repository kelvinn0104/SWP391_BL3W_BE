using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public interface IPasswordResetRepository
{
    Task AddAsync(PasswordReset reset, CancellationToken ct = default);
    Task<PasswordReset?> GetLatestActiveAsync(long userId, DateTime now, CancellationToken ct = default);
    Task<PasswordReset?> GetLatestVerifiedAsync(long userId, DateTime now, CancellationToken ct = default);
    Task UpdateAsync(PasswordReset reset, CancellationToken ct = default);
}
