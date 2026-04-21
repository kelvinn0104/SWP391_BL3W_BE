namespace WasteCollection_RecyclingPlatform.Services.DTOs;

public class RewardActionResult<T>
{
    public bool Success { get; set; }
    public bool Unauthorized { get; set; }
    public bool NotFound { get; set; }
    public string? Error { get; set; }
    public T? Data { get; set; }

    public static RewardActionResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static RewardActionResult<T> UnauthorizedResult(string? error = null) => new() { Unauthorized = true, Error = error };
    public static RewardActionResult<T> NotFoundResult(string? error = null) => new() { NotFound = true, Error = error };
}

public class PointBalanceResponse
{
    public int CurrentBalance { get; set; }
    public int Points { get; set; }
}

public class RewardPointTransactionItemResponse
{
    public long Id { get; set; }
    public int Amount { get; set; }
    public int BalanceAfter { get; set; }
    public string TransactionType { get; set; } = null!;
    public string SourceType { get; set; } = null!;
    public long? SourceRefId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class RewardPointHistoryResponse
{
    public int CurrentBalance { get; set; }
    public int TotalTransactions { get; set; }
    public List<RewardPointTransactionItemResponse> Transactions { get; set; } = new();
}

public class AreaLeaderboardItemResponse
{
    public int Rank { get; set; }
    public long AreaId { get; set; }
    public string AreaName { get; set; } = null!;
    public int TotalPoints { get; set; }
    public int ParticipantCount { get; set; }
}

public class AreaUserLeaderboardItemResponse
{
    public int Rank { get; set; }
    public long UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public int Points { get; set; }
    public int CompletedReports { get; set; }

    // Compatibility fields for the existing FE leaderboard shape.
    public string Name { get; set; } = null!;
    public string? Avatar { get; set; }
    public int Actions { get; set; }
}

public class AreaUserLeaderboardResponse
{
    public long AreaId { get; set; }
    public string AreaName { get; set; } = null!;
    public int TotalParticipants { get; set; }
    public int? MyRank { get; set; }
    public int? MyPoints { get; set; }
    public List<AreaUserLeaderboardItemResponse> Users { get; set; } = new();
}

public class UserLeaderboardResponse
{
    public int TotalParticipants { get; set; }
    public int? MyRank { get; set; }
    public int? MyPoints { get; set; }
    public List<AreaUserLeaderboardItemResponse> Users { get; set; } = new();
}
