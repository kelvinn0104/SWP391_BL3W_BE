using WasteCollection_RecyclingPlatform.Services.Model;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public interface IUserService
{
    Task<UserProfileResponse> GetProfileAsync(long userId, CancellationToken ct = default);
    Task<UserProfileResponse> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<List<UserProfileResponse>> GetCitizensAsync(CancellationToken ct = default);
    Task<List<UserProfileResponse>> GetCollectorsAsync(CancellationToken ct = default);
    Task<UserProfileResponse> CreateCollectorAsync(CollectorCreateRequest request, CancellationToken ct = default);
    Task<UserProfileResponse> UpdateAccountAsync(long userId, AccountUpdateRequest request, CancellationToken ct = default);
    Task DeleteAccountAsync(long userId, CancellationToken ct = default);
    Task UpdateAccountStatusAsync(long userId, bool isLocked, CancellationToken ct = default);
}
