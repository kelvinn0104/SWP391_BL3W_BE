using System.ComponentModel.DataAnnotations;

namespace WasteCollection_RecyclingPlatform.Services.Model;

public record CollectorCreateRequest(
    [property: Required][property: EmailAddress] string Email,
    [property: Required][property: MinLength(6)] string Password,
    [property: Required][property: Compare("Password")] string ConfirmPassword,
    string? DisplayName,
    string? FullName,
    [property: Required][property: Phone] string PhoneNumber,
    string? Address
);

public record AccountUpdateRequest(
    string? DisplayName,
    string? FullName,
    string? PhoneNumber,
    string? Address,
    string? Gender,
    DateTime? DateOfBirth,
    string? Language,
    string? AvatarUrl
);

public record AccountStatusRequest(
    [property: Required] bool IsLocked
);
