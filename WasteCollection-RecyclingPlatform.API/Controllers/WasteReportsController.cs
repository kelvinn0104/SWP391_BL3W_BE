using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WasteCollection_RecyclingPlatform.Services.DTOs;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/waste-reports")]
public class WasteReportsController : ControllerBase
{
    private readonly IWasteReportService _wasteReportService;

    public WasteReportsController(IWasteReportService wasteReportService)
    {
        _wasteReportService = wasteReportService;
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<List<WasteCategoryResponse>>> GetCategories(CancellationToken ct)
    {
        return Ok(await _wasteReportService.GetCategoriesAsync(ct));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<WasteReportResponse>> CreateReport([FromForm] WasteReportCreateRequest request, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var citizenId))
            return Unauthorized(new { message = "Không xác định được người dùng hiện tại." });

        var result = await _wasteReportService.CreateReportAsync(citizenId, request, ct);
        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return StatusCode(StatusCodes.Status201Created, result.Report);
    }

    private bool TryGetCurrentUserId(out long userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("id");

        return long.TryParse(raw, out userId);
    }
}
