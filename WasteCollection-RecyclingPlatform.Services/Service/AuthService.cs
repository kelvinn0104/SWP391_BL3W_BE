using Microsoft.Extensions.Configuration;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Repositories.Repository;
using WasteCollection_RecyclingPlatform.Services.Model;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordResetRepository _resetRepo;
    private readonly IJwtTokenService _jwt;
    private readonly IGoogleTokenVerifier _google;
    private readonly IEmailSender _email;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _config;

    public AuthService(
        IUserRepository userRepo,
        IPasswordResetRepository resetRepo,
        IJwtTokenService jwt,
        IGoogleTokenVerifier google,
        IEmailSender email,
        IPasswordHasher hasher,
        IConfiguration config)
    {
        _userRepo = userRepo;
        _resetRepo = resetRepo;
        _jwt = jwt;
        _google = google;
        _email = email;
        _hasher = hasher;
        _config = config;
    }


    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Email và mật khẩu là bắt buộc.");

        var user = await _userRepo.GetByEmailAsync(email, ct);
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        if (user.IsLocked)
            throw new UnauthorizedAccessException("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");

        return ToAuthResponse(user, _jwt.CreateToken(user));
    }


    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Email và mật khẩu là bắt buộc.");

        if (request.Password.Length < 6)
            throw new ArgumentException("Mật khẩu tối thiểu 6 ký tự.");

        if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            throw new ArgumentException("Email không hợp lệ.");

        if (displayName is { Length: > 100 })
            throw new ArgumentException("Tên hiển thị tối đa 100 ký tự.");

        string? phoneNumber = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phone = request.PhoneNumber.Trim();
            if (phone.Length != 10 || !phone.All(char.IsDigit))
                throw new ArgumentException("Số điện thoại phải bao gồm đúng 10 chữ số.");
            phoneNumber = phone;
        }

        var exists = await _userRepo.ExistsByEmailAsync(email, ct);
        if (exists)
            throw new InvalidOperationException("Email đã được sử dụng.");

        var user = new User
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = _hasher.Hash(request.Password),
            Role = UserRole.Citizen,
            Points = 0,
            PhoneNumber = phoneNumber,
        };

        await _userRepo.AddAsync(user, ct);
        return ToAuthResponse(user, _jwt.CreateToken(user));
    }


    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Credential))
            throw new ArgumentException("Thiếu credential (Google ID token).");

        GoogleUserInfo gUser;
        try
        {
            gUser = await _google.VerifyIdTokenAsync(request.Credential, ct);
        }
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException(ex.Message);
        }

        var email = gUser.Email.Trim().ToLowerInvariant();

        // GetByEmailAsync trả về AsNoTracking — cần tracked entity để update
        var user = await _userRepo.GetByEmailAsync(email, ct);
        if (user is null)
        {
            user = new User
            {
                Email = email,
                DisplayName = string.IsNullOrWhiteSpace(gUser.Name) ? null : gUser.Name.Trim(),
                PasswordHash = _hasher.Hash(Guid.NewGuid().ToString("N")),
                Role = UserRole.Citizen,
                Points = 0,
            };
            await _userRepo.AddAsync(user, ct);
        }
        else if (string.IsNullOrWhiteSpace(user.DisplayName) && !string.IsNullOrWhiteSpace(gUser.Name))
        {
            user.DisplayName = gUser.Name.Trim();
            await _userRepo.UpdateAsync(user, ct);
        }

        return ToAuthResponse(user, _jwt.CreateToken(user));
    }


    public async Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        const string safeMessage = "Nếu email tồn tại, mã xác nhận đã được gửi.";

        var email = Normalize(request.Email);
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email là bắt buộc.");

        var user = await _userRepo.GetByEmailAsync(email, ct);
        if (user is null)
            return new MessageResponse(safeMessage); // không tiết lộ email tồn tại

        var code = Random.Shared.Next(0, 1_000_000).ToString("D6");
        var reset = new PasswordReset
        {
            UserId = user.Id,
            CodeHash = _hasher.Hash(code),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            Attempts = 0,
        };
        await _resetRepo.AddAsync(reset, ct);

        var subject = "EcoSort - Mã xác nhận đặt lại mật khẩu";
        var logoUrl = (_config["Brand:LogoUrl"] ?? string.Empty).Trim();
        var logoSrc = string.IsNullOrWhiteSpace(logoUrl) ? "cid:ecosort-logo" : logoUrl;
        var body = BuildForgotPasswordEmail(code, logoSrc);

        await _email.SendAsync(email, subject, body, ct);
        return new MessageResponse(safeMessage);
    }


    public async Task<VerifyResetCodeResponse> VerifyResetCodeAsync(VerifyResetCodeRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Code))
            throw new ArgumentException("Email và mã xác nhận là bắt buộc.");

        var user = await _userRepo.GetByEmailAsync(email, ct);
        if (user is null)
            throw new UnauthorizedAccessException("Mã không hợp lệ hoặc đã hết hạn.");

        var now = DateTime.UtcNow;
        var reset = await _resetRepo.GetLatestActiveAsync(user.Id, now, ct);
        if (reset is null)
            throw new UnauthorizedAccessException("Mã không hợp lệ hoặc đã hết hạn.");

        reset.Attempts += 1;
        if (reset.Attempts > 5)
        {
            reset.UsedAtUtc = now;
            await _resetRepo.UpdateAsync(reset, ct);
            throw new UnauthorizedAccessException("Mã không hợp lệ hoặc đã hết hạn.");
        }

        if (!_hasher.Verify(request.Code.Trim(), reset.CodeHash))
        {
            await _resetRepo.UpdateAsync(reset, ct);
            throw new UnauthorizedAccessException("Mã không hợp lệ hoặc đã hết hạn.");
        }

        var token = Guid.NewGuid().ToString("N");
        reset.VerifiedAtUtc = now;
        reset.ResetTokenHash = _hasher.Hash(token);
        reset.ResetTokenExpiresAtUtc = now.AddMinutes(15);
        await _resetRepo.UpdateAsync(reset, ct);

        return new VerifyResetCodeResponse(token);
    }

    public async Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.ResetToken) || string.IsNullOrWhiteSpace(request.NewPassword))
            throw new ArgumentException("Thiếu thông tin.");

        if (request.NewPassword.Length < 6)
            throw new ArgumentException("Mật khẩu tối thiểu 6 ký tự.");

        var user = await _userRepo.GetByEmailAsync(email, ct);
        if (user is null)
            throw new UnauthorizedAccessException("Token không hợp lệ hoặc đã hết hạn.");

        var now = DateTime.UtcNow;
        var reset = await _resetRepo.GetLatestVerifiedAsync(user.Id, now, ct);
        if (reset is null || string.IsNullOrWhiteSpace(reset.ResetTokenHash))
            throw new UnauthorizedAccessException("Token không hợp lệ hoặc đã hết hạn.");

        if (!_hasher.Verify(request.ResetToken.Trim(), reset.ResetTokenHash))
            throw new UnauthorizedAccessException("Token không hợp lệ hoặc đã hết hạn.");

        // Cần tracked entity để update password
        var trackedUser = await _userRepo.GetByIdAsync(user.Id, ct);
        trackedUser!.PasswordHash = _hasher.Hash(request.NewPassword);
        reset.UsedAtUtc = now;

        await _userRepo.UpdateAsync(trackedUser, ct);
        await _resetRepo.UpdateAsync(reset, ct);

        return new MessageResponse("Đổi mật khẩu thành công.");
    }


    private static string Normalize(string? input)
        => (input ?? string.Empty).Trim().ToLowerInvariant();

    private static AuthResponse ToAuthResponse(User user, string token)
        => new AuthResponse(
            AccessToken: token,
            UserId: user.Id,
            Email: user.Email,
            DisplayName: user.DisplayName,
            Role: user.Role.ToString(),
            Points: user.Points,
            FullName: user.FullName,
            Gender: user.Gender,
            DateOfBirth: user.DateOfBirth,
            PhoneNumber: user.PhoneNumber,
            Address: user.Address,
            Language: user.Language,
            AvatarUrl: user.AvatarUrl
        );

    private static string BuildForgotPasswordEmail(string code, string logoSrc) => $"""
        <div style="margin:0;padding:0;background:#f6f8fb">
          <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse">
            <tr>
              <td align="center" style="padding:32px 16px">
                <table role="presentation" cellpadding="0" cellspacing="0" width="560" style="max-width:560px;width:100%;border-collapse:separate;border-spacing:0">
                  <tr>
                    <td style="padding:22px 24px;background:#0f172a;border-radius:18px 18px 0 0">
                      <div style="font-family:ui-sans-serif,system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial;display:flex;align-items:center;gap:10px">
                        <img src="{logoSrc}" width="36" height="36" alt="EcoSort" style="width:36px;height:36px;border-radius:12px;object-fit:cover;background:#ffffff" />
                        <div>
                          <div style="font-size:14px;font-weight:800;color:#e2e8f0;letter-spacing:.08em;text-transform:uppercase">EcoSort</div>
                          <div style="font-size:12px;color:#94a3b8;margin-top:2px">Waste Collection &amp; Recycling Platform</div>
                        </div>
                      </div>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:26px 24px 10px;background:#ffffff;border-left:1px solid #e5e7eb;border-right:1px solid #e5e7eb">
                      <div style="font-family:ui-sans-serif,system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial;color:#0f172a">
                        <h1 style="margin:0;font-size:20px;line-height:1.3;font-weight:900">Mã xác nhận đặt lại mật khẩu</h1>
                        <p style="margin:10px 0 0;color:#475569;font-size:14px;line-height:1.6">
                          Xin chào, bạn vừa yêu cầu đặt lại mật khẩu. Dùng mã dưới đây để xác nhận.
                        </p>
                      </div>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:18px 24px 8px;background:#ffffff;border-left:1px solid #e5e7eb;border-right:1px solid #e5e7eb">
                      <div style="font-family:ui-sans-serif,system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial;text-align:left">
                        <div style="display:inline-block;background:#f1f5f9;border:1px solid #e2e8f0;border-radius:16px;padding:16px 18px">
                          <div style="font-size:11px;letter-spacing:.2em;text-transform:uppercase;color:#64748b;font-weight:800">MÃ 6 SỐ</div>
                          <div style="margin-top:8px;font-size:28px;letter-spacing:.22em;font-weight:950;color:#0f172a">{code}</div>
                        </div>
                        <p style="margin:14px 0 0;color:#64748b;font-size:13px;line-height:1.6">
                          Mã có hiệu lực trong <b>10 phút</b>. Nếu bạn không yêu cầu, vui lòng bỏ qua email này.
                        </p>
                      </div>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:14px 24px 22px;background:#ffffff;border-left:1px solid #e5e7eb;border-right:1px solid #e5e7eb;border-bottom:1px solid #e5e7eb;border-radius:0 0 18px 18px">
                      <div style="font-family:ui-sans-serif,system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial;color:#94a3b8;font-size:12px;line-height:1.6">
                        <div>© {DateTime.UtcNow:yyyy} EcoSort</div>
                      </div>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </div>
        """;
}
