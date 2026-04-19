using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var result = await _collectorJobService.GetMyJobsAsync(collectorId, null, ct);
        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Jobs);
    }

    [HttpGet("{reportId:long}/detail")]
    public async Task<ActionResult<CollectorJobResponse>> GetMyJobDetail(long reportId, CancellationToken ct)
    {
        if (!_collectorJobService.TryGetCurrentUserId(User, out var collectorId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var result = await _collectorJobService.GetMyJobDetailAsync(collectorId, reportId, ct);
        if (result.NotFound)
            return NotFound();

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Job);
    }

    [HttpPatch("{reportId:long}/status")]
    public async Task<ActionResult<CollectorJobResponse>> UpdateMyJobStatus(
        long reportId,
        [FromBody] CollectorJobStatusUpdateRequest request,
        CancellationToken ct)
    {
        if (!_collectorJobService.TryGetCurrentUserId(User, out var collectorId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var result = await _collectorJobService.UpdateMyJobStatusAsync(collectorId, reportId, request, ct);
        if (result.NotFound)
            return NotFound();

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Job);
    }

    [HttpPost("{reportId:long}/complete")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CollectorJobResponse>> CompleteMyJob(
        long reportId,
        [FromForm] CollectorJobCompletionRequest request,
        CancellationToken ct)
    {
        var formBindResult = _collectorJobService.BindCompletionRequestFromRawForm(
            request,
            Request.HasFormContentType ? Request.Form : null);

        if (!formBindResult.Success)
            return BadRequest(new { message = formBindResult.Error });

        if (!_collectorJobService.TryGetCurrentUserId(User, out var collectorId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var result = await _collectorJobService.CompleteMyJobAsync(collectorId, reportId, request, ct);
        if (result.NotFound)
            return NotFound();

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Job);
    }
}
