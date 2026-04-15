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

    public UsersController(IUserService userService)
    {
        _userService = userService;
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
        return Ok(await _userService.GetCollectorsAsync(ct));
    }
}
