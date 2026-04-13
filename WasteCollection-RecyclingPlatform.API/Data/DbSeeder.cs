using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Data;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        await EnsureUserAsync(db,
            email: "admin@gmail.com",
            displayName: "Admin",
            password: "123456",
            role: UserRole.Administrator);

        await EnsureUserAsync(db,
            email: "enterprise@gmail.com",
            displayName: "Recycling Enterprise",
            password: "123456",
            role: UserRole.RecyclingEnterprise);

        await EnsureUserAsync(db,
            email: "collector@gmail.com",
            displayName: "Professional Collector",
            password: "123456",
            role: UserRole.Collector);
    }

    private static async Task EnsureUserAsync(
        AppDbContext db,
        string email,
        string displayName,
        string password,
        UserRole role)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var existing = await db.Users.FirstOrDefaultAsync(x => x.Email == normalized);
        if (existing is not null)
        {
            if (existing.Role == default) existing.Role = role;
            if (string.IsNullOrWhiteSpace(existing.DisplayName)) existing.DisplayName = displayName;
            await db.SaveChangesAsync();
            return;
        }

        // Dùng BCrypt trực tiếp để tránh phụ thuộc circular — Seeder không đi qua Service layer
        db.Users.Add(new User
        {
            Email = normalized,
            DisplayName = displayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            Points = 1250,
        });
        await db.SaveChangesAsync();
    }
}
