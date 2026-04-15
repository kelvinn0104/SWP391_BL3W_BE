using WasteCollection_RecyclingPlatform.Repositories.Repository;
using WasteCollection_RecyclingPlatform.Services.Model;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;

    public UserService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<UserProfileResponse> GetProfileAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        return MapToProfileResponse(user);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        user.DisplayName = request.DisplayName;
        user.FullName = request.FullName;
        user.Gender = request.Gender;
        user.DateOfBirth = request.DateOfBirth;
        user.PhoneNumber = request.PhoneNumber;
        user.Address = request.Address;
        user.Language = request.Language;
        if (!string.IsNullOrEmpty(request.AvatarUrl))
            user.AvatarUrl = request.AvatarUrl;

        await _userRepo.UpdateAsync(user, ct);
        return MapToProfileResponse(user);
    }

    public async Task<List<UserProfileResponse>> GetCollectorsAsync(CancellationToken ct = default)
    {
        var users = await _userRepo.GetAllAsync(ct);
        return users
            .Where(u => u.Role == Repositories.Entities.UserRole.Collector)
            .Select(MapToProfileResponse)
            .ToList();
    }

    private UserProfileResponse MapToProfileResponse(Repositories.Entities.User user)
    {
        return new UserProfileResponse(
            UserId: user.Id,
            Email: user.Email,
            DisplayName: user.DisplayName,
            FullName: user.FullName,
            Gender: user.Gender,
            DateOfBirth: user.DateOfBirth,
            PhoneNumber: user.PhoneNumber,
            Address: user.Address,
            Language: user.Language,
            AvatarUrl: user.AvatarUrl,
            Role: user.Role.ToString(),
            Points: user.Points
        );
    }
}
