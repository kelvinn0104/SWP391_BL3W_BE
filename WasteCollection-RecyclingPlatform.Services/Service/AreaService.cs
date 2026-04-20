using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Data;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Repositories.Repository;
using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public class AreaService : IAreaService
{
    private readonly IAreaRepository _areaRepository;
    private readonly AppDbContext _db; // Used for lookups during mapping if needed, or inject IUserRepository

    public AreaService(IAreaRepository areaRepository, AppDbContext db)
    {
        _areaRepository = areaRepository;
        _db = db;
    }

    public async Task<List<AreaResponse>> GetAllAreasAsync(CancellationToken ct = default)
    {
        var areas = await _areaRepository.GetAllAsync(ct);

        return areas.Select(a => new AreaResponse
        {
            Id = a.Id.ToString(),
            District = a.DistrictName,
            MonthlyCapacityKg = a.MonthlyCapacityKg,
            ProcessedThisMonthKg = a.ProcessedThisMonthKg,
            CompletedRequests = a.CompletedRequests,
            Wards = a.Wards.Select(w => new WardResponse
            {
                Id = w.Id,
                Name = w.Name,
                CollectedKg = w.CollectedKg,
                CompletedRequests = w.CompletedRequests,
                Collectors = w.Collectors.Select(c => c.DisplayName ?? c.Email).ToList()
            }).ToList()
        }).ToList();
    }

    public async Task<List<AreaResponse>> BulkUpdateAreasAsync(List<AreaResponse> areasDto, CancellationToken ct = default)
    {
        var areas = new List<Area>();

        foreach (var aDto in areasDto)
        {
            var area = new Area
            {
                DistrictName = aDto.District,
                MonthlyCapacityKg = aDto.MonthlyCapacityKg,
                ProcessedThisMonthKg = aDto.ProcessedThisMonthKg,
                CompletedRequests = aDto.CompletedRequests,
                Wards = new List<Ward>()
            };

            foreach (var wDto in aDto.Wards)
            {
                var ward = new Ward
                {
                    Name = wDto.Name,
                    CollectedKg = wDto.CollectedKg,
                    CompletedRequests = wDto.CompletedRequests,
                    Collectors = new List<User>()
                };

                foreach (var collectorName in wDto.Collectors)
                {
                    var user = await _db.Users.FirstOrDefaultAsync(u => u.DisplayName == collectorName || u.Email == collectorName, ct);
                    if (user != null)
                    {
                        ward.Collectors.Add(user);
                    }
                }

                area.Wards.Add(ward);
            }

            areas.Add(area);
        }

        await _areaRepository.BulkUpdateAsync(areas, ct);
        return await GetAllAreasAsync(ct);
    }

    public async Task<bool> DeleteAreaAsync(string id, CancellationToken ct = default)
    {
        if (!long.TryParse(id, out var areaId)) return false;
        return await _areaRepository.DeleteAsync(areaId, ct);
    }

    public async Task<bool> UpdateAreaInfoAsync(string id, AreaResponse dto, CancellationToken ct = default)
    {
        if (!long.TryParse(id, out var areaId)) return false;
        return await _areaRepository.UpdateNameAndCapacityAsync(areaId, dto.District, dto.MonthlyCapacityKg, ct);
    }
}
