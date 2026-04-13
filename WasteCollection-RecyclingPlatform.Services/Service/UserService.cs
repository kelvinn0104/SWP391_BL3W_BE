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

        return new UserProfileResponse(user.Id, user.Email, user.DisplayName, user.Role.ToString(), user.Points);
    }
}
