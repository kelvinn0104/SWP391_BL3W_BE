namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class User
{
    public long Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Language { get; set; }
    public string? AvatarUrl { get; set; }

    public UserRole Role { get; set; } = UserRole.Citizen;
    public int Points { get; set; } = 0;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Ward> Wards { get; set; } = new List<Ward>();
}
