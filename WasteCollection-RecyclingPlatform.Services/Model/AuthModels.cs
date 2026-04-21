using System;
using Microsoft.AspNetCore.Http;

namespace WasteCollection_RecyclingPlatform.Services.Model;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string Password, string? DisplayName);

public record GoogleLoginRequest(string Credential);

public record ForgotPasswordRequest(string Email);

public record VerifyResetCodeRequest(string Email, string Code);

public record ResetPasswordRequest(string Email, string ResetToken, string NewPassword);

public record AuthResponse(
    string AccessToken, 
    long UserId, 
    string Email, 
    string? DisplayName, 
    string Role, 
    int Points,
    string? FullName = null,
    string? Gender = null,
    DateTime? DateOfBirth = null,
    string? PhoneNumber = null,
    string? Address = null,
    string? Language = null,
    string? AvatarUrl = null
);

public record VerifyResetCodeResponse(string ResetToken);

public record MessageResponse(string Message);

public record UserProfileResponse(
    long UserId, 
    string Email, 
    string? DisplayName, 
    string? FullName,
    string? Gender,
    DateTime? DateOfBirth,
    string? PhoneNumber,
    string? Address,
    string? Language,
    string? AvatarUrl,
    string Role, 
    int Points,
    bool IsLocked,
    List<long> WardIds
);

public class UpdateProfileRequest
{
    public string? DisplayName { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Language { get; set; }
    public string? AvatarUrl { get; set; }
    public IFormFile? AvatarFile { get; set; }
}
