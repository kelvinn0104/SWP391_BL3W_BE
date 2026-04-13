using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
