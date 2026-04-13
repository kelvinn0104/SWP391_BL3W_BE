using WasteCollection_RecyclingPlatform.Services.Model;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default);
    Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task<VerifyResetCodeResponse> VerifyResetCodeAsync(VerifyResetCodeRequest request, CancellationToken ct = default);
    Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}
