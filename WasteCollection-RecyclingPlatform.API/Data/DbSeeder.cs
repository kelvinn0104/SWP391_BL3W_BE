using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Data;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

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
            role: UserRole.Collector,
            phoneNumber: "0901234567");

        for (int i = 1; i <= 100; i++)
        {
            await EnsureUserAsync(db,
                email: $"collector{i}@gmail.com",
                displayName: $"Collector #{i}",
                password: "123456",
                role: UserRole.Collector,
                phoneNumber: $"09{i:D8}");
        }

        await SeedAreasAsync(db);
        await SeedVouchersAsync(db);
    }

    private static async Task SeedAreasAsync(AppDbContext db)
    {
        // Clear all existing data to ensure a fresh HCMC dataset
        if (await db.Areas.AnyAsync())
        {
            db.Areas.RemoveRange(db.Areas);
            await db.SaveChangesAsync();
            Console.WriteLine("[Seeder] Cleared existing Area/Ward data.");
        }

        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "hcmc_admin_units.json");
        if (!File.Exists(jsonPath)) jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "hcmc_admin_units.json");
        
        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"[Seeder] Warning: Could not find HCMC data file at {jsonPath}");
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var data = JsonSerializer.Deserialize<HcmcData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (data?.Districts == null) return;

        var allCollectors = await db.Users
            .Where(u => u.Role == UserRole.Collector)
            .ToListAsync();
        var rnd = new Random();

        var areas = new List<Area>();
        foreach (var d in data.Districts)
        {
            var area = new Area
            {
                DistrictName = d.Name,
                MonthlyCapacityKg = 0, // Calculated later
                ProcessedThisMonthKg = 0,
                CompletedRequests = 0,
                Wards = new List<Ward>()
            };

            foreach (var w in d.Wards)
            {
                var ward = new Ward
                {
                    Name = w.Name,
                    CollectedKg = rnd.Next(500, 2501), // 500 to 2500 Kg
                    CompletedRequests = rnd.Next(20, 151), // 20 to 150 requests
                    Collectors = new List<User>()
                };
                
                // Randomly assign 1-3 collectors from our pool
                if (allCollectors.Any())
                {
                    int count = rnd.Next(1, 4); 
                    var assigned = allCollectors.OrderBy(x => rnd.Next()).Take(count).ToList();
                    foreach(var coll in assigned) ward.Collectors.Add(coll);
                }

                area.Wards.Add(ward);
            }

            // Calculate area totals based on wards
            area.ProcessedThisMonthKg = area.Wards.Sum(w => w.CollectedKg);
            area.CompletedRequests = area.Wards.Sum(w => w.CompletedRequests);
            
            // Set dynamic capacity slightly above current performance
            area.MonthlyCapacityKg = area.ProcessedThisMonthKg + rnd.Next(2000, 5001);
            
            areas.Add(area);
        }

        db.Areas.AddRange(areas);
        await db.SaveChangesAsync();
        Console.WriteLine($"[Seeder] Successfully seeded {areas.Count} districts and {areas.Sum(a => a.Wards.Count)} wards with random test data.");
    }

    private static async Task EnsureUserAsync(
        AppDbContext db,
        string email,
        string displayName,
        string password,
        UserRole role,
        string? phoneNumber = null)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var existing = await db.Users.FirstOrDefaultAsync(x => x.Email == normalized);
        if (existing is not null)
        {
            if (existing.Role == default) existing.Role = role;
            if (string.IsNullOrWhiteSpace(existing.DisplayName)) existing.DisplayName = displayName;
            if (!string.IsNullOrWhiteSpace(phoneNumber) && string.IsNullOrWhiteSpace(existing.PhoneNumber))
                existing.PhoneNumber = phoneNumber;

            await db.SaveChangesAsync();
            return;
        }

        db.Users.Add(new User
        {
            Email = normalized,
            DisplayName = displayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            PhoneNumber = phoneNumber,
            Points = 1250,
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedVouchersAsync(AppDbContext db)
    {
        if (await db.VoucherCategories.AnyAsync()) return;

        var foodCat = new VoucherCategory { Name = "Ẩm thực" };
        var shopCat = new VoucherCategory { Name = "Mua sắm" };
        var moveCat = new VoucherCategory { Name = "Di chuyển" };

        db.VoucherCategories.AddRange(foodCat, shopCat, moveCat);
        await db.SaveChangesAsync();

        var vouchers = new List<Voucher>
        {
            new Voucher 
            { 
                Title = "Voucher Highland Coffee 50k", 
                PointsRequired = 500, 
                CategoryId = foodCat.Id,
                ImageUrl = "/voucher/voucher-1.jpg",
                Codes = new List<VoucherCode>
                {
                    new VoucherCode { Code = "HL-ABC-123" },
                    new VoucherCode { Code = "HL-DEF-456" },
                    new VoucherCode { Code = "HL-GHI-789" }
                }
            },
            new Voucher 
            { 
                Title = "Voucher Shopee 100k", 
                PointsRequired = 1000, 
                CategoryId = shopCat.Id,
                ImageUrl = "/voucher/voucher-2.jpg",
                Codes = new List<VoucherCode>
                {
                    new VoucherCode { Code = "SHP-SALE-100" },
                    new VoucherCode { Code = "SHP-SALE-200" }
                }
            },
            new Voucher 
            { 
                Title = "GrabRide Discount 20k", 
                PointsRequired = 200, 
                CategoryId = moveCat.Id,
                ImageUrl = "/voucher/voucher-3.jpg",
                Codes = new List<VoucherCode>
                {
                    new VoucherCode { Code = "GRAB-20K-1" },
                    new VoucherCode { Code = "GRAB-20K-2" },
                    new VoucherCode { Code = "GRAB-20K-3" },
                    new VoucherCode { Code = "GRAB-20K-4" }
                }
            }
        };

        db.Vouchers.AddRange(vouchers);
        await db.SaveChangesAsync();
        Console.WriteLine("[Seeder] Successfully seeded Voucher data.");
    }

    private class HcmcData
    {
        public List<DistrictData> Districts { get; set; } = new();
    }

    private class DistrictData
    {
        public string Name { get; set; } = string.Empty;
        public List<WardData> Wards { get; set; } = new();
    }

    private class WardData
    {
        public string Name { get; set; } = string.Empty;
    }
}
