using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Data;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email, ct);

    public async Task<User?> GetByIdAsync(long id, CancellationToken ct = default)
        => await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Users.AsNoTracking().AnyAsync(x => x.Email == email, ct);

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }
}
