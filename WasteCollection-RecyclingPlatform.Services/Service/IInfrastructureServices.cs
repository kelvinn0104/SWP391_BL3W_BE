using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Services.Service;


/// Tạo JWT access token cho user đã xác thực.
/// Implementation nằm ở API layer (JwtTokenService).
public interface IJwtTokenService
{
    string CreateToken(User user);
}

/// Xác thực Google ID token và trả về thông tin user từ Google.
public record GoogleUserInfo(string Email, string? Name);

public interface IGoogleTokenVerifier
{
    Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken, CancellationToken ct = default);
}

/// Gửi email HTML.
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}

/// Hash và verify password.
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
