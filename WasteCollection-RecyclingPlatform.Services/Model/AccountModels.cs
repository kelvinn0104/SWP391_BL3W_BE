using System.ComponentModel.DataAnnotations;

namespace WasteCollection_RecyclingPlatform.Services.Model;

public class CollectorCreateRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = null!;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = null!;

    public string? DisplayName { get; set; }
    public string? FullName { get; set; }

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = null!;

    public string? Address { get; set; }
}

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
    [Required] bool IsLocked
);
