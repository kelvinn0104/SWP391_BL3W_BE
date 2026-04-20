using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WasteCollection_RecyclingPlatform.Services.Model;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize(Roles = "Administrator,RecyclingEnterprise")]
public class AccountController : ControllerBase
{
    private readonly IUserService _userService;

    public AccountController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("citizens")]
    public async Task<ActionResult<List<UserProfileResponse>>> GetCitizens(CancellationToken ct)
    {
        var citizens = await _userService.GetCitizensAsync(ct);
        return Ok(citizens);
    }

    [HttpGet("collectors")]
    public async Task<ActionResult<List<UserProfileResponse>>> GetCollectors([FromQuery] long? wardId, CancellationToken ct)
    {
        var collectors = await _userService.GetCollectorsAsync(wardId: wardId, ct: ct);
        return Ok(collectors);
    }

    [HttpPost("collectors")]
    public async Task<ActionResult<UserProfileResponse>> CreateCollector([FromBody] CollectorCreateRequest request, CancellationToken ct)
    {
        var collector = await _userService.CreateCollectorAsync(request, ct);
        return CreatedAtAction(nameof(GetCollectors), collector);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserProfileResponse>> UpdateAccount(long id, [FromBody] AccountUpdateRequest request, CancellationToken ct)
    {
        var updated = await _userService.UpdateAccountAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(long id, CancellationToken ct)
    {
        await _userService.DeleteAccountAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] AccountStatusRequest request, CancellationToken ct)
    {
        await _userService.UpdateAccountStatusAsync(id, request.IsLocked, ct);
        return Ok(new { Message = request.IsLocked ? "Tài khoản đã được khóa." : "Tài khoản đã được mở khóa." });
    }
}
