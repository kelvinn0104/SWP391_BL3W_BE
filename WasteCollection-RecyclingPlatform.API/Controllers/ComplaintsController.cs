using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Services.DTOs;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/feedback")]
public class ComplaintsController : ControllerBase
{
    private readonly IComplaintService _complaintService;

    public ComplaintsController(IComplaintService complaintService)
    {
        _complaintService = complaintService;
    }

    [HttpPost("reports/{reportId:long}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ComplaintResponse>> CreateForReport(long reportId, [FromForm] ComplaintCreateRequest request, CancellationToken ct)
    {
        if (!_complaintService.TryGetCurrentUserId(User, out var citizenId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var result = await _complaintService.CreateComplaintAsync(citizenId, reportId, request, ct);
        if (result.Unauthorized)
            return Unauthorized(new { message = result.Error });

        if (result.NotFound)
            return NotFound(new { message = result.Error });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<ComplaintResponse>>> GetMyFeedback(CancellationToken ct)
    {
        if (!_complaintService.TryGetCurrentUserId(User, out var citizenId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var result = await _complaintService.GetMyComplaintsAsync(citizenId, ct);
        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data ?? new List<ComplaintResponse>());
    }

    [HttpGet]
    [Authorize(Roles = "Administrator,RecyclingEnterprise")]
    public async Task<ActionResult<List<ComplaintResponse>>> GetFeedback([FromQuery] ComplaintStatus? status, CancellationToken ct)
    {
        var result = await _complaintService.GetComplaintsAsync(status, ct);
        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data ?? new List<ComplaintResponse>());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ComplaintResponse>> GetFeedbackDetail(long id, CancellationToken ct)
    {
        if (!_complaintService.TryGetCurrentUserId(User, out var actorUserId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var canViewAll = User.IsInRole("Administrator") || User.IsInRole("RecyclingEnterprise");
        var result = await _complaintService.GetComplaintDetailAsync(actorUserId, canViewAll, id, ct);
        if (result.NotFound)
            return NotFound(new { message = result.Error });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPut("{id:long}/status")]
    [Authorize(Roles = "Administrator,RecyclingEnterprise")]
    public async Task<ActionResult<ComplaintResponse>> UpdateFeedbackStatus(long id, [FromBody] ComplaintStatusUpdateRequest request, CancellationToken ct)
    {
        if (!_complaintService.TryGetCurrentUserId(User, out var actorUserId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var result = await _complaintService.UpdateStatusAsync(actorUserId, id, request, ct);
        if (result.NotFound)
            return NotFound(new { message = result.Error });

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }
}
