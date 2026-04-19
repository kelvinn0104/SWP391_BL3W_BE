using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Services.DTOs;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Controllers;

[Authorize(Roles = "Collector")]
[ApiController]
[Route("api/collector/jobs")]
public class CollectorJobsController : ControllerBase
{
    private readonly ICollectorJobService _collectorJobService;

    public CollectorJobsController(ICollectorJobService collectorJobService)
    {
        _collectorJobService = collectorJobService;
    }

    [HttpGet("collector-report-assigned")]
    public async Task<ActionResult<List<CollectorJobSummaryResponse>>> GetMyAssignedReports(CancellationToken ct)
    {
        if (!_collectorJobService.TryGetCurrentUserId(User, out var collectorId))
            return Unauthorized(new { message = "Cannot identify current user." });

        var result = await _collectorJobService.GetMyJobsAsync(collectorId, null, ct);
        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Jobs);
    }

    [HttpGet("{reportId:long}/detail")]
    public async Task<ActionResult<CollectorJobResponse>> GetMyJobDetail(long reportId, CancellationToken ct)
    {
        if (!_collectorJobService.TryGetCurrentUserId(User, out var collectorId))
            return Unauthorized(new { message = "Cannot identify current user." });

        var result = await _collectorJobService.GetMyJobDetailAsync(collectorId, reportId, ct);
        if (result.NotFound)
            return NotFound();

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Job);
    }
}
