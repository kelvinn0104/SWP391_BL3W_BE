namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class PasswordReset
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string CodeHash { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
    public int Attempts { get; set; } = 0;
    public DateTime? VerifiedAtUtc { get; set; }
    public string? ResetTokenHash { get; set; }
    public DateTime? ResetTokenExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAtUtc { get; set; }
}
