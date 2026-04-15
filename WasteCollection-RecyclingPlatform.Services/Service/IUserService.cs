using WasteCollection_RecyclingPlatform.Services.Model;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public interface IUserService
{
    Task<UserProfileResponse> GetProfileAsync(long userId, CancellationToken ct = default);
    Task<UserProfileResponse> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<List<UserProfileResponse>> GetCollectorsAsync(CancellationToken ct = default);
}
