using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WasteCollection_RecyclingPlatform.Services.DTOs;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly IRewardService _rewardService;

    public LeaderboardController(IRewardService rewardService)
    {
        _rewardService = rewardService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AreaUserLeaderboardItemResponse>>> GetUserLeaderboard([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        if (!_rewardService.TryGetCurrentUserId(User, out var userId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var response = await _rewardService.GetUserLeaderboardAsync(userId, skip, take, ct);
        if (response.Unauthorized)
            return Unauthorized(new { message = response.Error });

        return Ok(response.Data?.Users ?? new List<AreaUserLeaderboardItemResponse>());
    }

    [HttpGet("users")]
    public async Task<ActionResult<UserLeaderboardResponse>> GetUsersLeaderboard([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        if (!_rewardService.TryGetCurrentUserId(User, out var userId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var response = await _rewardService.GetUserLeaderboardAsync(userId, skip, take, ct);
        if (response.Unauthorized)
            return Unauthorized(new { message = response.Error });

        return Ok(response.Data);
    }

    [HttpGet("areas")]
    public async Task<ActionResult<List<AreaLeaderboardItemResponse>>> GetAreaLeaderboard([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        return Ok(await _rewardService.GetAreaLeaderboardAsync(skip, take, ct));
    }

    [HttpGet("areas/{areaId:long}/users")]
    public async Task<ActionResult<AreaUserLeaderboardResponse>> GetAreaUsersLeaderboard(long areaId, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        if (!_rewardService.TryGetCurrentUserId(User, out var userId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var response = await _rewardService.GetAreaUserLeaderboardAsync(areaId, userId, skip, take, ct);
        if (response.Unauthorized)
            return Unauthorized(new { message = response.Error });

        if (response.NotFound)
            return NotFound(new { message = response.Error });

        return Ok(response.Data);
    }
}
