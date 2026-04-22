using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Repositories.Repository;
using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public class WasteCategoryService : IWasteCategoryService
{
    private readonly IWasteCategoryRepository _repo;

    public WasteCategoryService(IWasteCategoryRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<WasteCategoryDetailResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await _repo.GetAllAsync(ct);
        return categories.Select(Map).ToList();
    }

    public async Task<WasteCategoryDetailResponse?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var cat = await _repo.GetByIdAsync(id, ct);
        return cat is null ? null : Map(cat);
    }

    public async Task<WasteCategoryResult> CreateAsync(WasteCategoryCreateRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _repo.ExistsByCodeAsync(code, null, ct))
            return WasteCategoryResult.Fail($"Mã danh mục '{code}' đã tồn tại.");

        var category = new WasteCategory
        {
            Code = code,
            Name = request.Name.Trim(),
            Unit = string.IsNullOrWhiteSpace(request.Unit) ? "kg" : request.Unit.Trim(),
            Description = request.Description?.Trim(),
            PointsPerKg = request.PointsPerKg,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _repo.AddAsync(category, ct);
        return WasteCategoryResult.Ok(Map(category));
    }

    public async Task<WasteCategoryResult> UpdatePointsAsync(long id, WasteCategoryUpdatePointsRequest request, CancellationToken ct = default)
    {
        var cat = await _repo.GetByIdAsync(id, ct);
        if (cat is null)
            return WasteCategoryResult.NotFoundResult();

        cat.PointsPerKg = request.PointsPerKg;
        cat.UpdatedAtUtc = DateTime.UtcNow;

        await _repo.SaveChangesAsync(ct);
        return WasteCategoryResult.Ok(Map(cat));
    }

    public async Task<WasteCategoryResult> ToggleActiveAsync(long id, CancellationToken ct = default)
    {
        var cat = await _repo.GetByIdAsync(id, ct);
        if (cat is null)
            return WasteCategoryResult.NotFoundResult();

        cat.IsActive = !cat.IsActive;
        cat.UpdatedAtUtc = DateTime.UtcNow;

        await _repo.SaveChangesAsync(ct);
        return WasteCategoryResult.Ok(Map(cat));
    }

    private static WasteCategoryDetailResponse Map(WasteCategory cat) => new()
    {
        Id = cat.Id,
        Code = cat.Code,
        Name = cat.Name,
        Unit = cat.Unit,
        Description = cat.Description,
        PointsPerKg = cat.PointsPerKg,
        IsActive = cat.IsActive,
        CreatedAtUtc = cat.CreatedAtUtc,
        UpdatedAtUtc = cat.UpdatedAtUtc,
    };
}
