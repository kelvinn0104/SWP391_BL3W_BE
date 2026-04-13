using Microsoft.AspNetCore.Mvc;
using WasteCollection_RecyclingPlatform.Services.Model;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        try { return Ok(await _authService.LoginAsync(req, ct)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        try { return Ok(await _authService.RegisterAsync(req, ct)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest req, CancellationToken ct)
    {
        try { return Ok(await _authService.GoogleLoginAsync(req, ct)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req, CancellationToken ct)
    {
        try { return Ok(await _authService.ForgotPasswordAsync(req, ct)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("verify-reset-code")]
    public async Task<ActionResult<VerifyResetCodeResponse>> VerifyResetCode([FromBody] VerifyResetCodeRequest req, CancellationToken ct)
    {
        try { return Ok(await _authService.VerifyResetCodeAsync(req, ct)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        try { return Ok(await _authService.ResetPasswordAsync(req, ct)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }
}
