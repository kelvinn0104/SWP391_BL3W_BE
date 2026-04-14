using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Data;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Repository;

public class AreaRepository : IAreaRepository
{
    private readonly AppDbContext _db;

    public AreaRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Area>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Areas
            .Include(a => a.Wards)
                .ThenInclude(w => w.Collectors)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task BulkUpdateAsync(List<Area> areas, CancellationToken ct = default)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var existingAreas = await _db.Areas
                .Include(a => a.Wards)
                    .ThenInclude(w => w.Collectors)
                .ToListAsync(ct);

            var newAreaNames = areas.Select(a => a.DistrictName).ToList();
            var areasToRemove = existingAreas.Where(a => !newAreaNames.Contains(a.DistrictName)).ToList();
            _db.Areas.RemoveRange(areasToRemove);

            foreach (var areaData in areas)
            {
                var area = existingAreas.FirstOrDefault(a => a.DistrictName == areaData.DistrictName);
                if (area == null)
                {
                    area = new Area
                    {
                        DistrictName = areaData.DistrictName,
                        MonthlyCapacityKg = areaData.MonthlyCapacityKg,
                        ProcessedThisMonthKg = areaData.ProcessedThisMonthKg,
                        CompletedRequests = areaData.CompletedRequests
                    };
                    _db.Areas.Add(area);
                }
                else
                {
                    area.MonthlyCapacityKg = areaData.MonthlyCapacityKg;
                    area.ProcessedThisMonthKg = areaData.ProcessedThisMonthKg;
                    area.CompletedRequests = areaData.CompletedRequests;
                }

                if (areaData.Wards == null) continue;

                // Sync Wards
                var existingWards = area.Wards.ToList();
                var newWardNames = areaData.Wards.Select(w => w.Name).ToList();

                var wardsToRemove = existingWards.Where(w => !newWardNames.Contains(w.Name)).ToList();
                _db.Wards.RemoveRange(wardsToRemove);

                foreach (var wardData in areaData.Wards)
                {
                    var ward = existingWards.FirstOrDefault(w => w.Name == wardData.Name);
                    if (ward == null)
                    {
                        ward = new Ward
                        {
                            Name = wardData.Name,
                            CollectedKg = wardData.CollectedKg,
                            CompletedRequests = wardData.CompletedRequests,
                            Area = area
                        };
                        _db.Wards.Add(ward);
                    }
                    else
                    {
                        ward.CollectedKg = wardData.CollectedKg;
                        ward.CompletedRequests = wardData.CompletedRequests;
                    }

                    // Sync Collectors
                    if (ward.Collectors == null) ward.Collectors = new List<User>();
                    ward.Collectors.Clear();
                    
                    if (wardData.Collectors != null)
                    {
                        foreach (var collector in wardData.Collectors)
                        {
                            User? user = null;
                            if (collector.Id > 0)
                            {
                                user = await _db.Users.FindAsync(new object[] { collector.Id }, ct);
                            }
                            else if (!string.IsNullOrEmpty(collector.DisplayName))
                            {
                                user = await _db.Users.FirstOrDefaultAsync(u => u.DisplayName == collector.DisplayName, ct);
                            }

                            if (user != null && !ward.Collectors.Contains(user))
                            {
                                ward.Collectors.Add(user);
                            }
                        }
                    }
                }

                // Strictly recalculate totals from wards to ensure accurate rankings
                area.ProcessedThisMonthKg = area.Wards.Sum(w => w.CollectedKg);
                area.CompletedRequests = area.Wards.Sum(w => w.CompletedRequests);
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        var area = await _db.Areas.FindAsync(new object[] { id }, ct);
        if (area == null) return false;

        _db.Areas.Remove(area);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateNameAndCapacityAsync(long id, string newName, decimal newCapacity, CancellationToken ct = default)
    {
        var area = await _db.Areas.FindAsync(new object[] { id }, ct);
        if (area == null) return false;

        area.DistrictName = newName;
        area.MonthlyCapacityKg = newCapacity;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}
