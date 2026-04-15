namespace WasteCollection_RecyclingPlatform.Services.Model;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string Password, string? DisplayName);

public record GoogleLoginRequest(string Credential);

public record ForgotPasswordRequest(string Email);

public record VerifyResetCodeRequest(string Email, string Code);

public record ResetPasswordRequest(string Email, string ResetToken, string NewPassword);

public record AuthResponse(string AccessToken, long UserId, string Email, string? DisplayName, string Role, int Points);

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
    int Points
);

public record UpdateProfileRequest(
    string? DisplayName,
    string? FullName,
    string? Gender,
    DateTime? DateOfBirth,
    string? PhoneNumber,
    string? Address,
    string? Language,
    string? AvatarUrl
);
