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
        await SeedWasteCategoriesAsync(db);
        await SeedRewardSamplesAsync(db);
        await SeedComplaintSamplesAsync(db);

        // 2. Automatic Repair & Sync (The "Self-Healing" logic)
        await RepairDataAsync(db);
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

    private static async Task SeedWasteCategoriesAsync(AppDbContext db)
    {
        var existingCategories = await db.WasteCategories.ToListAsync();

        var now = DateTime.UtcNow;
        if (!existingCategories.Any())
        {
            db.WasteCategories.AddRange(
            new WasteCategory
            {
                Code = "PLASTIC",
                Name = "Nhựa",
                Unit = "kg",
                Description = "Chai, hộp nhựa, túi nilon đã làm sạch.",
                PointsPerKg = 100,
                CreatedAtUtc = now,
            },
            new WasteCategory
            {
                Code = "METAL",
                Name = "Kim loại",
                Unit = "kg",
                Description = "Lon, vỏ hộp và phế liệu kim loại.",
                PointsPerKg = 120,
                CreatedAtUtc = now,
            },
            new WasteCategory
            {
                Code = "GENERAL",
                Name = "Rác chung",
                Unit = "kg",
                Description = "Các loại rác tái chế thông thường không thuộc nhóm nhựa hoặc kim loại.",
                PointsPerKg = 80,
                CreatedAtUtc = now,
            });
        }
        else
        {
            var generalWasteCategory = existingCategories.FirstOrDefault(x => x.Code == "GENERAL");
            var legacyGeneralWasteCategory = existingCategories.FirstOrDefault(x => x.Code == "PAPER");

            if (generalWasteCategory is null && legacyGeneralWasteCategory is not null)
            {
                legacyGeneralWasteCategory.Code = "GENERAL";
                generalWasteCategory = legacyGeneralWasteCategory;
            }
            else if (generalWasteCategory is not null && legacyGeneralWasteCategory is not null)
            {
                var legacyGeneralWasteItems = await db.WasteReportItems
                    .Where(x => x.WasteCategoryId == legacyGeneralWasteCategory.Id)
                    .ToListAsync();

                foreach (var item in legacyGeneralWasteItems)
                    item.WasteCategoryId = generalWasteCategory.Id;

                db.WasteCategories.Remove(legacyGeneralWasteCategory);
            }

            if (generalWasteCategory is not null)
            {
                generalWasteCategory.Name = "Rác chung";
                generalWasteCategory.Description = "Các loại rác tái chế thông thường không thuộc nhóm nhựa hoặc kim loại.";
                generalWasteCategory.UpdatedAtUtc = now;
            }
        }

        var removedCategories = existingCategories.Where(x => x.Code is "GLASS" or "EWASTE").ToList();
        if (removedCategories.Any())
        {
            var fallbackCategory = existingCategories.FirstOrDefault(x => x.Code == "PLASTIC")
                ?? existingCategories.First(x => x.Code is "METAL" or "GENERAL");
            var removedCategoryIds = removedCategories.Select(x => x.Id).ToList();
            var affectedItems = await db.WasteReportItems
                .Where(x => removedCategoryIds.Contains(x.WasteCategoryId))
                .ToListAsync();

            foreach (var item in affectedItems)
                item.WasteCategoryId = fallbackCategory.Id;

            db.WasteCategories.RemoveRange(removedCategories);
        }

        await db.SaveChangesAsync();
        Console.WriteLine("[Seeder] Successfully seeded waste categories.");
    }


    private static async Task SeedRewardSamplesAsync(AppDbContext db)
    {
        if (await db.RewardPointTransactions.AnyAsync(x => x.Description != null && x.Description.StartsWith("[Seeder]")))
            return;

        var sampleCitizens = new[]
        {
            new { Email = "citizen@gmail.com", Name = "Nguyen Van Dan", Phone = "0988776655", Points = 980 },
            new { Email = "tran.anh@gmail.com", Name = "Tran Thi Anh", Phone = "0912345678", Points = 1670 },
            new { Email = "le.hoang@gmail.com", Name = "Le Minh Hoang", Phone = "0933445566", Points = 1250 },
            new { Email = "pham.lan@gmail.com", Name = "Pham Huong Lan", Phone = "0944556677", Points = 760 },
            new { Email = "vo.thanh@gmail.com", Name = "Vo Thanh Trung", Phone = "0955667788", Points = 2140 },
        };

        foreach (var citizen in sampleCitizens)
        {
            await EnsureUserAsync(db, citizen.Email, citizen.Name, "123456", UserRole.Citizen, citizen.Phone);
        }

        var citizens = await db.Users
            .Include(u => u.Wards)
            .Where(u => sampleCitizens.Select(c => c.Email).Contains(u.Email))
            .ToListAsync();

        foreach (var citizen in citizens)
        {
            var sample = sampleCitizens.First(x => x.Email == citizen.Email);
            citizen.DisplayName = sample.Name;
            citizen.PhoneNumber = sample.Phone;
            citizen.Points = sample.Points;
        }

        var wards = await db.Wards.Take(citizens.Count).ToListAsync();
        for (var i = 0; i < citizens.Count && i < wards.Count; i++)
        {
            if (!citizens[i].Wards.Any())
                citizens[i].Wards.Add(wards[i]);
        }

        var categories = await db.WasteCategories.ToListAsync();
        if (!categories.Any())
        {
            await db.SaveChangesAsync();
            return;
        }

        var actor = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Collector)
            ?? await db.Users.FirstAsync(u => u.Role == UserRole.Administrator);

        var plastic = categories.FirstOrDefault(x => x.Code == "PLASTIC") ?? categories[0];
        var generalWaste = categories.FirstOrDefault(x => x.Code == "GENERAL") ?? categories[0];
        var metal = categories.FirstOrDefault(x => x.Code == "METAL") ?? categories[0];
        var now = DateTime.UtcNow;

        var samplePlans = new[]
        {
            new { Email = "vo.thanh@gmail.com", Category = metal, Weight = 8.0m, Points = 960, DaysAgo = 2 },
            new { Email = "vo.thanh@gmail.com", Category = metal, Weight = 4.0m, Points = 480, DaysAgo = 8 },
            new { Email = "tran.anh@gmail.com", Category = plastic, Weight = 7.5m, Points = 750, DaysAgo = 3 },
            new { Email = "tran.anh@gmail.com", Category = generalWaste, Weight = 5.0m, Points = 400, DaysAgo = 9 },
            new { Email = "le.hoang@gmail.com", Category = plastic, Weight = 6.5m, Points = 650, DaysAgo = 4 },
            new { Email = "citizen@gmail.com", Category = generalWaste, Weight = 5.0m, Points = 400, DaysAgo = 5 },
            new { Email = "pham.lan@gmail.com", Category = generalWaste, Weight = 4.5m, Points = 360, DaysAgo = 6 },
        };

        foreach (var plan in samplePlans)
        {
            var citizen = citizens.FirstOrDefault(x => x.Email == plan.Email);
            if (citizen is null) continue;

            var collectedAt = now.AddDays(-plan.DaysAgo);
            var assignedAt = collectedAt.AddHours(-2);
            var report = new WasteReport
            {
                CitizenId = citizen.Id,
                Title = $"[Seeder] Reward sample report for {citizen.DisplayName}",
                Description = "Sample collected report used to test reward points and leaderboard APIs.",
                LocationText = "Sample location, Ho Chi Minh City",
                AssignedCollectorId = actor.Id,
                AssignedAtUtc = assignedAt,
                Status = WasteReportStatus.Collected,
                EstimatedTotalPoints = plan.Points,
                FinalRewardPoints = plan.Points,
                RewardVerifiedAtUtc = collectedAt,
                ActualTotalWeightKg = plan.Weight,
                CompletedAtUtc = collectedAt,
                CompletionNote = "[Seeder] Collector confirmed sample collection.",
                CreatedAtUtc = collectedAt.AddHours(-6),
                UpdatedAtUtc = collectedAt,
                Items = new List<WasteReportItem>
                {
                    new WasteReportItem
                    {
                        WasteCategoryId = plan.Category.Id,
                        EstimatedWeightKg = plan.Weight,
                        ActualWeightKg = plan.Weight,
                        EstimatedPoints = plan.Points,
                    }
                },
                StatusHistories = new List<WasteReportStatusHistory>
                {
                    new WasteReportStatusHistory
                    {
                        Status = WasteReportStatus.Pending,
                        ChangedByUserId = citizen.Id,
                        ChangedAtUtc = collectedAt.AddHours(-6),
                        Note = "[Seeder] Citizen created sample reward report.",
                    },
                    new WasteReportStatusHistory
                    {
                        Status = WasteReportStatus.Assigned,
                        ChangedByUserId = actor.Id,
                        ChangedAtUtc = assignedAt,
                        Note = "[Seeder] Sample report assigned.",
                    },
                    new WasteReportStatusHistory
                    {
                        Status = WasteReportStatus.Accepted,
                        ChangedByUserId = actor.Id,
                        ChangedAtUtc = assignedAt.AddMinutes(15),
                        Note = "[Seeder] Collector accepted sample report.",
                    },
                    new WasteReportStatusHistory
                    {
                        Status = WasteReportStatus.Collected,
                        ChangedByUserId = actor.Id,
                        ChangedAtUtc = collectedAt,
                        Note = "[Seeder] Sample report collected.",
                    },
                },
            };

            db.WasteReports.Add(report);
            await db.SaveChangesAsync();

            db.RewardPointTransactions.Add(new RewardPointTransaction
            {
                UserId = citizen.Id,
                Amount = plan.Points,
                BalanceAfter = citizen.Points,
                TransactionType = RewardPointTransactionType.Earned,
                SourceType = RewardPointSourceType.WasteReportCollected,
                SourceRefId = report.Id,
                Description = $"[Seeder] Reward points for collected sample report #{report.Id}",
                CreatedByUserId = actor.Id,
                CreatedAtUtc = collectedAt,
            });
        }

        await db.SaveChangesAsync();
        Console.WriteLine("[Seeder] Successfully seeded reward sample reports and point transactions.");
    }

    private static async Task SeedComplaintSamplesAsync(AppDbContext db)
    {
        if (await db.Complaints.AnyAsync())
            return;

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Administrator);
        var collector = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Collector);
        var categories = await db.WasteCategories.ToListAsync();
        var fallbackCategory = categories.FirstOrDefault();

        var complaintReports = await db.WasteReports
            .Include(r => r.Citizen)
            .Where(r => r.Status == WasteReportStatus.Collected)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(3)
            .ToListAsync();

        if (fallbackCategory is not null)
        {
            var cancelledCitizen = await db.Users.FirstOrDefaultAsync(u => u.Email == "citizen@gmail.com" && u.Role == UserRole.Citizen);
            if (cancelledCitizen is not null)
            {
                var cancelledAt = DateTime.UtcNow.AddDays(-1);
                var cancelledReport = new WasteReport
                {
                    CitizenId = cancelledCitizen.Id,
                    Title = "[Seeder] Cancelled report for complaint sample",
                    Description = "Sample cancelled report used to test complaint feedback APIs.",
                    LocationText = "123 Nguyen Van Linh, TP.HCM",
                    Status = WasteReportStatus.Cancelled,
                    EstimatedTotalPoints = 250,
                    CreatedAtUtc = cancelledAt.AddHours(-4),
                    UpdatedAtUtc = cancelledAt,
                    Items = new List<WasteReportItem>
                    {
                        new WasteReportItem
                        {
                            WasteCategoryId = fallbackCategory.Id,
                            EstimatedWeightKg = 2.5m,
                            EstimatedPoints = 250,
                        }
                    },
                    StatusHistories = new List<WasteReportStatusHistory>
                    {
                        new WasteReportStatusHistory
                        {
                            Status = WasteReportStatus.Pending,
                            ChangedByUserId = cancelledCitizen.Id,
                            ChangedAtUtc = cancelledAt.AddHours(-4),
                            Note = "[Seeder] Citizen created report for complaint sample.",
                        },
                        new WasteReportStatusHistory
                        {
                            Status = WasteReportStatus.Cancelled,
                            ChangedByUserId = admin?.Id ?? collector?.Id,
                            ChangedAtUtc = cancelledAt,
                            Note = "[Seeder] Sample report cancelled because collection commitment was not met.",
                        },
                    },
                };

                db.WasteReports.Add(cancelledReport);
                await db.SaveChangesAsync();
                complaintReports.Add(cancelledReport);
            }
        }

        var now = DateTime.UtcNow;
        var samples = complaintReports
            .Select((report, index) => new Complaint
            {
                WasteReportId = report.Id,
                CitizenId = report.CitizenId,
                Reason = index switch
                {
                    0 => "Thu gom không đúng địa điểm",
                    1 => "Thu gom thiếu / không đúng loại rác",
                    2 => "Trạng thái cập nhật sai",
                    _ => "Nhân viên thu gom thái độ không phù hợp",
                },
                Description = index switch
                {
                    0 => "Nhân viên đến nhầm địa chỉ nên gia đình phải tự mang rác tái chế ra điểm hẹn khác.",
                    1 => "Báo cáo có nhựa và rác chung nhưng đội thu gom chỉ nhận phần nhựa, phần rác chung vẫn còn lại.",
                    2 => "Ứng dụng hiển thị đã thu gom, tuy nhiên thực tế rác vẫn chưa được lấy trong khung giờ cam kết.",
                    _ => "Lịch thu gom bị hủy sát giờ và không có thông báo rõ ràng cho công dân.",
                },
                Status = index switch
                {
                    0 => ComplaintStatus.Submitted,
                    1 => ComplaintStatus.InReview,
                    2 => ComplaintStatus.Resolved,
                    _ => ComplaintStatus.Rejected,
                },
                AdminNote = index switch
                {
                    1 => "Đã chuyển bộ phận điều phối kiểm tra lại hình ảnh và biên bản thu gom.",
                    2 => "Đã xác minh và cập nhật lại quy trình xác nhận thu gom cho collector.",
                    3 => "Không đủ bằng chứng để xác nhận vi phạm cam kết thu gom.",
                    _ => null,
                },
                ResolvedByUserId = index is 2 or 3 ? admin?.Id : null,
                ResolvedAtUtc = index is 2 or 3 ? now.AddHours(-index) : null,
                CreatedAtUtc = now.AddDays(-(index + 1)).AddHours(-2),
                UpdatedAtUtc = index == 0 ? null : now.AddDays(-index),
                EvidenceFiles = index == 0
                    ? new List<ComplaintEvidence>
                    {
                        new ComplaintEvidence
                        {
                            FileUrl = "/complaint-evidence/sample-feedback-evidence.jpg",
                            OriginalFileName = "sample-feedback-evidence.jpg",
                            ContentType = "image/jpeg",
                            UploadedAtUtc = now.AddDays(-1),
                        }
                    }
                    : new List<ComplaintEvidence>(),
            })
            .ToList();

        db.Complaints.AddRange(samples);
        await db.SaveChangesAsync();
        Console.WriteLine($"[Seeder] Successfully seeded {samples.Count} complaint feedback samples.");
    }

    private static async Task RepairDataAsync(AppDbContext db)
    {
        Console.WriteLine("[Seeder] Starting Automatic Data Repair...");
        var rnd = new Random();

        // A. Ensure all collectors are assigned to at least one ward
        var collectors = await db.Users
            .Include(u => u.Wards)
            .Where(u => u.Role == UserRole.Collector)
            .ToListAsync();

        var allWards = await db.Wards.ToListAsync();

        if (allWards.Any())
        {
            foreach (var col in collectors)
            {
                if (!col.Wards.Any())
                {
                    // Assign to 1-2 random wards if orphaned
                    int count = rnd.Next(1, 4);
                    var wardsToAssign = allWards.OrderBy(x => rnd.Next()).Take(count).ToList();
                    foreach (var w in wardsToAssign) col.Wards.Add(w);
                    Console.WriteLine($"[Seeder] Auto-assigned {col.DisplayName} to {count} wards.");
                }
            }
        }

        // B. (Legacy request repair removed)

        // C. Backfill legacy collected waste reports after UC-COL-02 completion fields were added.
        var collectedReports = await db.WasteReports
            .Include(r => r.Items)
            .Include(r => r.StatusHistories)
            .Where(r => r.Status == WasteReportStatus.Collected)
            .ToListAsync();

        foreach (var report in collectedReports)
        {
            var changed = false;
            var histories = report.StatusHistories
                .OrderBy(h => h.ChangedAtUtc)
                .ThenBy(h => h.Id)
                .ToList();

            var assignedHistory = histories.LastOrDefault(h => h.Status == WasteReportStatus.Assigned);
            var collectedHistory = histories.LastOrDefault(h => h.Status == WasteReportStatus.Collected);

            if (assignedHistory is not null
                && collectedHistory is not null
                && histories.All(h => h.Status != WasteReportStatus.Accepted || h.ChangedAtUtc <= assignedHistory.ChangedAtUtc))
            {
                report.StatusHistories.Add(new WasteReportStatusHistory
                {
                    Status = WasteReportStatus.Accepted,
                    ChangedByUserId = assignedHistory.ChangedByUserId,
                    ChangedAtUtc = assignedHistory.ChangedAtUtc.AddTicks(
                        Math.Max(1, (collectedHistory.ChangedAtUtc - assignedHistory.ChangedAtUtc).Ticks / 3)),
                    Note = "[Seeder] Bổ sung trạng thái Accepted sau Assigned cho dữ liệu thu gom cũ.",
                });
                changed = true;
            }

            if (!report.CompletedAtUtc.HasValue)
            {
                report.CompletedAtUtc = collectedHistory?.ChangedAtUtc ?? report.UpdatedAtUtc ?? report.CreatedAtUtc;
                changed = true;
            }

            foreach (var item in report.Items.Where(i => !i.ActualWeightKg.HasValue && i.EstimatedWeightKg.HasValue))
            {
                item.ActualWeightKg = item.EstimatedWeightKg;
                changed = true;
            }

            if (!report.ActualTotalWeightKg.HasValue)
            {
                var actualWeights = report.Items
                    .Select(i => i.ActualWeightKg)
                    .Where(w => w.HasValue)
                    .Select(w => w!.Value)
                    .ToList();

                if (actualWeights.Count > 0)
                {
                    report.ActualTotalWeightKg = actualWeights.Sum();
                    changed = true;
                }
            }

            if (string.IsNullOrWhiteSpace(report.CompletionNote))
            {
                report.CompletionNote = "[Seeder] Bổ sung thông tin hoàn tất cho dữ liệu thu gom cũ.";
                changed = true;
            }

            if (changed)
                Console.WriteLine($"[Seeder] Backfilled completion data for WasteReport #{report.Id}.");
        }

        await db.SaveChangesAsync();
        Console.WriteLine("[Seeder] Data Repair Complete.");
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
