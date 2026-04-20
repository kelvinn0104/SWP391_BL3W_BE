using System.IO;
using Microsoft.AspNetCore.Http;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Repositories.Repository;
using WasteCollection_RecyclingPlatform.Services.Model;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;

    public UserService(IUserRepository userRepo, IPasswordHasher hasher)
    {
        _userRepo = userRepo;
        _hasher = hasher;
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

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phone = request.PhoneNumber.Trim();
            if (phone.Length != 10 || !phone.All(char.IsDigit))
                throw new ArgumentException("Số điện thoại phải bao gồm đúng 10 chữ số.");
            
            user.PhoneNumber = phone;
        }

        user.DisplayName = request.DisplayName;
        user.FullName = request.FullName;
        user.Gender = request.Gender;
        user.DateOfBirth = request.DateOfBirth;
        user.Address = request.Address;
        user.Language = request.Language;
        if (request.AvatarFile != null)
        {
            user.AvatarUrl = await SaveProfileAvatarAsync(request.AvatarFile);
        }
        else if (!string.IsNullOrEmpty(request.AvatarUrl))
            user.AvatarUrl = request.AvatarUrl;

        await _userRepo.UpdateAsync(user, ct);
        return MapToProfileResponse(user);
    }

    public async Task<List<UserProfileResponse>> GetCitizensAsync(CancellationToken ct = default)
    {
        var users = await _userRepo.GetByRoleAsync(UserRole.Citizen, null, ct);
        return users.Select(MapToProfileResponse).ToList();
    }

    public async Task<List<UserProfileResponse>> GetCollectorsAsync(long? wardId = null, CancellationToken ct = default)
    {
        var users = await _userRepo.GetByRoleAsync(UserRole.Collector, wardId, ct);
        return users.Select(MapToProfileResponse).ToList();
    }

    public async Task<UserProfileResponse> CreateCollectorAsync(CollectorCreateRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _userRepo.ExistsByEmailAsync(email, ct))
            throw new InvalidOperationException("Email đã được sử dụng.");

        var user = new User
        {
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            DisplayName = request.DisplayName,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Role = UserRole.Collector,
            Points = 0
        };

        await _userRepo.AddAsync(user, ct);
        return MapToProfileResponse(user);
    }

    public async Task<UserProfileResponse> UpdateAccountAsync(long userId, AccountUpdateRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null)
            throw new KeyNotFoundException("Người dùng không tồn tại.");

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phone = request.PhoneNumber.Trim();
            if (phone.Length != 10 || !phone.All(char.IsDigit))
                throw new ArgumentException("Số điện thoại phải bao gồm đúng 10 chữ số.");
            
            user.PhoneNumber = phone;
        }

        user.DisplayName = request.DisplayName ?? user.DisplayName;
        user.FullName = request.FullName ?? user.FullName;
        user.Gender = request.Gender ?? user.Gender;
        user.DateOfBirth = request.DateOfBirth ?? user.DateOfBirth;
        user.Address = request.Address ?? user.Address;
        user.Language = request.Language ?? user.Language;
        if (request.AvatarFile != null)
        {
            user.AvatarUrl = await SaveProfileAvatarAsync(request.AvatarFile);
        }
        else if (!string.IsNullOrEmpty(request.AvatarUrl))
            user.AvatarUrl = request.AvatarUrl;

        await _userRepo.UpdateAsync(user, ct);
        return MapToProfileResponse(user);
    }

    private async Task<string> SaveProfileAvatarAsync(IFormFile file)
    {
        // Primary path confirmed for the user's environment
        string fePublicPath = @"d:\WasteCollection-RecyclingPlatform\WasteCollection-RecyclingPlatform.FE\public\profile";
        
        // Backup discovery logic
        if (!Directory.Exists(fePublicPath))
        {
            var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (currentDir != null)
            {
                if (currentDir.Name == "WasteCollection-RecyclingPlatform")
                {
                    fePublicPath = Path.Combine(currentDir.FullName, "WasteCollection-RecyclingPlatform.FE", "public", "profile");
                    break;
                }
                currentDir = currentDir.Parent;
            }
        }

        if (!Directory.Exists(fePublicPath)) 
        {
            Directory.CreateDirectory(fePublicPath);
        }

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(fePublicPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return "/profile/" + fileName;
    }

    public async Task DeleteAccountAsync(long userId, CancellationToken ct = default)
    {
        await _userRepo.DeleteAsync(userId, ct);
    }

    public async Task UpdateAccountStatusAsync(long userId, bool isLocked, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null)
            throw new KeyNotFoundException("Người dùng không tồn tại.");

        user.IsLocked = isLocked;
        await _userRepo.UpdateAsync(user, ct);
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
            Points: user.Points,
            IsLocked: user.IsLocked,
            WardIds: user.Wards?.Select(w => w.Id).ToList() ?? new List<long>()
        );
    }
}
