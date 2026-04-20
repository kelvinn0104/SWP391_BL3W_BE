using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WasteCollection_RecyclingPlatform.Services.Model;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IRewardService _rewardService;

    public UsersController(IUserService userService, IRewardService rewardService)
    {
        _userService = userService;
        _rewardService = rewardService;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> Me(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!long.TryParse(sub, out var userId))
            return Unauthorized();

        try { return Ok(await _userService.GetProfileAsync(userId, ct)); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!long.TryParse(sub, out var userId))
            return Unauthorized();

        try { return Ok(await _userService.UpdateProfileAsync(userId, request, ct)); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
    }

    [HttpGet("collectors")]
    public async Task<ActionResult<List<UserProfileResponse>>> GetCollectors(CancellationToken ct)
    {
        return Ok(await _userService.GetCollectorsAsync(null, ct));
    }

    [Authorize]
    [HttpGet("points/history")]
    public async Task<ActionResult<object>> GetPointHistory([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        if (!_rewardService.TryGetCurrentUserId(User, out var userId))
            return Unauthorized(new { message = "Cannot identify current user." });

        var response = await _rewardService.GetPointHistoryAsync(userId, skip, take, ct);
        if (response.Unauthorized)
            return Unauthorized(new { message = response.Error });

        return Ok(response.Data);
    }

    [Authorize]
    [HttpGet("points/now")]
    public async Task<ActionResult<object>> GetPointBalance(CancellationToken ct)
    {
        if (!_rewardService.TryGetCurrentUserId(User, out var userId))
            return Unauthorized(new { message = "Cannot identify current user." });

        var response = await _rewardService.GetPointBalanceAsync(userId, ct);
        if (response.Unauthorized)
            return Unauthorized(new { message = response.Error });

        return Ok(response.Data);
    }
}
