using System.Text.Json;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Repositories.Repository;
using WasteCollection_RecyclingPlatform.Services.Model;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public class CollectionService : ICollectionService
{
    private readonly ICollectionRequestRepository _requestRepo;
    private readonly IUserRepository _userRepo;

    public CollectionService(ICollectionRequestRepository requestRepo, IUserRepository userRepo)
    {
        _requestRepo = requestRepo;
        _userRepo = userRepo;
    }

    public async Task<List<CollectionRequestResponse>> GetAllRequestsAsync(CancellationToken ct = default)
    {
        var requests = await _requestRepo.GetAllAsync(ct);
        return requests.Select(MapToResponse).ToList();
    }

    public async Task<CollectionRequestResponse?> GetRequestByIdAsync(long id, CancellationToken ct = default)
    {
        var request = await _requestRepo.GetByIdAsync(id, ct);
        return request == null ? null : MapToResponse(request);
    }

    public async Task<bool> UpdateStatusAsync(long id, CollectionRequestStatus status, string? reason = null, CancellationToken ct = default)
    {
        var request = await _requestRepo.GetByIdAsync(id, ct);
        if (request == null) return false;

        request.Status = status;
        if (status == CollectionRequestStatus.Cancelled)
        {
            request.CancellationReason = reason;
        }
        else if (status == CollectionRequestStatus.Collected)
        {
            request.CompletedAtUtc = DateTime.UtcNow;
        }
        else if (status == CollectionRequestStatus.Accepted || status == CollectionRequestStatus.Pending)
        {
            // Clear assignment if moved back to early states
            request.CollectorId = null;
            request.CollectorName = null;
            request.CollectorPhone = null;
        }

        await _requestRepo.UpdateAsync(request, ct);
        return true;
    }

    public async Task<bool> AssignCollectorAsync(long id, long collectorId, CancellationToken ct = default)
    {
        var request = await _requestRepo.GetByIdAsync(id, ct);
        if (request == null) return false;

        var collector = await _userRepo.GetByIdAsync(collectorId);
        if (collector == null || collector.Role != UserRole.Collector) return false;

        request.CollectorId = collectorId;
        request.CollectorName = collector.DisplayName ?? collector.FullName;
        request.CollectorPhone = collector.PhoneNumber;
        request.Status = CollectionRequestStatus.Assigned;

        await _requestRepo.UpdateAsync(request, ct);
        return true;
    }

    public async Task<List<CollectionRequestResponse>> GetCitizenRequestsAsync(long citizenId, CancellationToken ct = default)
    {
        var requests = await _requestRepo.GetByCitizenIdAsync(citizenId, ct);
        return requests.Select(MapToResponse).ToList();
    }

    public async Task<List<CollectionRequestResponse>> GetCollectorRequestsAsync(long collectorId, CancellationToken ct = default)
    {
        var requests = await _requestRepo.GetByCollectorIdAsync(collectorId, ct);
        return requests.Select(MapToResponse).ToList();
    }

    private CollectionRequestResponse MapToResponse(CollectionRequest req)
    {
        var materials = new List<WasteMaterialDto>();
        if (!string.IsNullOrEmpty(req.MaterialsJson))
        {
            try { materials = JsonSerializer.Deserialize<List<WasteMaterialDto>>(req.MaterialsJson) ?? new(); } catch { }
        }

        var images = new List<string>();
        if (!string.IsNullOrEmpty(req.ImagesJson))
        {
            try { images = JsonSerializer.Deserialize<List<string>>(req.ImagesJson) ?? new(); } catch { }
        }

        return new CollectionRequestResponse(
            req.Id,
            req.CitizenId,
            req.CitizenName ?? req.Citizen?.DisplayName ?? "Khách vãng lai",
            req.CollectorId,
            req.CollectorName ?? req.Collector?.DisplayName,
            req.CollectorPhone ?? req.Collector?.PhoneNumber,
            req.Address,
            req.WasteType,
            req.WeightKg,
            req.Note,
            req.Priority,
            req.Status.ToString(),
            req.CreatedAtUtc,
            req.CompletedAtUtc,
            req.CancellationReason,
            req.WardId,
            req.Ward?.Name,
            materials,
            images
        );
    }
}
