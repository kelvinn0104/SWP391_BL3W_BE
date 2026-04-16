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
        => await _db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);

    public async Task<User?> GetByIdAsync(long id, CancellationToken ct = default)
        => await _db.Users.Include(u => u.Wards).FirstOrDefaultAsync(x => x.Id == id, ct);

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

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, ct);
        if (user != null)
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<List<User>> GetAllAsync(CancellationToken ct = default)
        => await _db.Users.AsNoTracking().ToListAsync(ct);

    public async Task<List<User>> GetByRoleAsync(UserRole role, long? wardId = null, CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking()
            .Include(u => u.Wards)
            .Where(u => u.Role == role);
            
        if (wardId.HasValue)
        {
            query = query.Where(u => u.Wards.Any(w => w.Id == wardId.Value));
        }
        
        return await query.ToListAsync(ct);
    }
}
