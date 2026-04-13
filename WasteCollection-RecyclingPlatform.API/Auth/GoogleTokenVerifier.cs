using Google.Apis.Auth;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Auth;

public class GoogleTokenVerifier : IGoogleTokenVerifier
{
    private readonly string _clientId;

    public GoogleTokenVerifier(IConfiguration config)
    {
        _clientId = config["GoogleAuth:ClientId"] ?? string.Empty;
    }

    public async Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_clientId) || _clientId.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("GoogleAuth:ClientId chưa được cấu hình trong appsettings.");

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { _clientId }
        });

        return new GoogleUserInfo(payload.Email, payload.Name);
    }
}
