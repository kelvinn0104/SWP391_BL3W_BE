namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public enum RewardPointTransactionType
{
    Earned = 1,
    Spent = 2,
    Adjustment = 3,
}

public enum RewardPointSourceType
{
    WasteReportCollected = 1,
    VoucherRedemption = 2,
    ManualAdjustment = 3,
}

public class RewardPointTransaction
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public int Amount { get; set; }
    public int BalanceAfter { get; set; }
    public RewardPointTransactionType TransactionType { get; set; } = RewardPointTransactionType.Earned;
    public RewardPointSourceType SourceType { get; set; } = RewardPointSourceType.WasteReportCollected;
    public long? SourceRefId { get; set; }
    public string? Description { get; set; }

    public long? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}