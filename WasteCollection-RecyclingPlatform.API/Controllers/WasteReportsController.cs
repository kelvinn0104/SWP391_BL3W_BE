using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Services.DTOs;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/waste-reports")]
public class WasteReportsController : ControllerBase
{
    private readonly IWasteReportService _wasteReportService;
    private readonly ICollectorJobService _collectorJobService;

    public WasteReportsController(IWasteReportService wasteReportService, ICollectorJobService collectorJobService)
    {
        _wasteReportService = wasteReportService;
        _collectorJobService = collectorJobService;
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<List<WasteCategoryResponse>>> GetCategories(CancellationToken ct)
    {
        return Ok(await _wasteReportService.GetCategoriesAsync(ct));
    }

    [HttpGet("my-reports")]
    public async Task<ActionResult<List<WasteReportResponse>>> GetMyReports(CancellationToken ct)
    {
        if (!_wasteReportService.TryGetCurrentUserId(User, out var citizenId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        return Ok(await _wasteReportService.GetCitizenReportsAsync(citizenId, ct));
    }

    [HttpGet("get-all")]
    [Authorize(Roles = "RecyclingEnterprise")]
    public async Task<ActionResult<List<WasteReportResponse>>> GetAllReports(CancellationToken ct)
    {
        if (!_wasteReportService.TryGetCurrentUserId(User, out var currentUserId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var canViewAllReports = User.IsInRole(UserRole.Administrator.ToString()) || User.IsInRole(UserRole.RecyclingEnterprise.ToString());
        return Ok(await _wasteReportService.GetReportsAsync(currentUserId, canViewAllReports, ct));
    }

    [HttpGet("{id:long}/detail-report")]
    public async Task<ActionResult<WasteReportResponse>> GetDetailReport(long id, CancellationToken ct)
    {
        if (!_wasteReportService.TryGetCurrentUserId(User, out var citizenId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var report = await _wasteReportService.GetCitizenReportDetailAsync(citizenId, id, ct);
        if (report is null) return NotFound();

        return Ok(report);
    }

    [HttpGet("search-report-status")]
    [Authorize(Roles = "Citizen,Administrator,RecyclingEnterprise")]
    public async Task<ActionResult<List<WasteReportResponse>>> SearchReportsByStatus([FromQuery] WasteReportStatus status, CancellationToken ct)
    {
        if (!_wasteReportService.TryGetCurrentUserId(User, out var currentUserId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var canViewAllReports = User.IsInRole(UserRole.Administrator.ToString()) || User.IsInRole(UserRole.RecyclingEnterprise.ToString());
        var reports = await _wasteReportService.SearchReportsByStatusAsync(currentUserId, canViewAllReports, status, ct);
        if (reports is null)
            return BadRequest(new { message = "Trạng thái báo cáo không hợp lệ. Các giá trị hợp lệ: Pending, Accepted, Assigned, Collected, Cancelled." });

        return Ok(reports);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<WasteReportResponse>> CreateReport([FromForm] WasteReportCreateRequest request, CancellationToken ct)
    {
        var formItemsResult = _wasteReportService.BindWasteItemsFromRawForm(
            request,
            Request.HasFormContentType ? Request.Form : null);

        if (!formItemsResult.Success)
            return BadRequest(new { message = formItemsResult.Error });

        if (!_wasteReportService.TryGetCurrentUserId(User, out var citizenId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var result = await _wasteReportService.CreateReportAsync(citizenId, request, ct);
        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return StatusCode(StatusCodes.Status201Created, result.Report);
    }

    [HttpPut("{id:long}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<WasteReportResponse>> UpdateReport(long id, [FromForm] WasteReportUpdateRequest request, CancellationToken ct)
    {
        var formItemsResult = _wasteReportService.BindWasteItemsFromRawForm(
            request,
            Request.HasFormContentType ? Request.Form : null);

        if (!formItemsResult.Success)
            return BadRequest(new { message = formItemsResult.Error });

        if (!_wasteReportService.TryGetCurrentUserId(User, out var citizenId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var result = await _wasteReportService.UpdateReportAsync(citizenId, id, request, ct);
        if (result.NotFound)
            return NotFound();

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Report);
    }

    [HttpPost("{id:long}/advance-status")]
    [Authorize(Roles = "Administrator,RecyclingEnterprise")]
    public async Task<ActionResult<WasteReportStatusTrackingResponse>> AdvanceStatus(long id, [FromBody] WasteReportStatusActionRequest? request, CancellationToken ct)
    {
        if (!_wasteReportService.TryGetCurrentUserId(User, out var actorUserId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var result = await _wasteReportService.AdvanceReportStatusAsync(actorUserId, id, request?.Note, ct);
        if (result.NotFound)
            return NotFound();

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Tracking);
    }

    [HttpGet("{id:long}/status-history")]
    [Authorize(Roles = "Administrator,RecyclingEnterprise")]
    public async Task<ActionResult<WasteReportStatusTrackingResponse>> GetStatusHistory(long id, CancellationToken ct)
    {
        var tracking = await _wasteReportService.GetReportStatusTrackingAsync(id, ct);
        if (tracking is null)
            return NotFound();

        return Ok(tracking);
    }

    [HttpPatch("{id:long}/assign-collector")]
    [Authorize(Roles = "Administrator,RecyclingEnterprise")]
    public async Task<ActionResult<CollectorJobResponse>> AssignCollector(long id, [FromBody] AssignWasteReportCollectorRequest request, CancellationToken ct)
    {
        if (!_collectorJobService.TryGetCurrentUserId(User, out var actorUserId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var result = await _collectorJobService.AssignCollectorAsync(actorUserId, id, request.CollectorId, ct);
        if (result.NotFound)
            return NotFound();

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Job);
    }

    [HttpPost("{id:long}/cancel")]
    [Authorize(Roles = "Administrator,RecyclingEnterprise")]
    public async Task<ActionResult<WasteReportStatusTrackingResponse>> CancelReport(long id, [FromBody] WasteReportStatusActionRequest? request, CancellationToken ct)
    {
        if (!_wasteReportService.TryGetCurrentUserId(User, out var actorUserId))
            return Unauthorized(new { message = "Không thể xác định người dùng hiện tại." });

        var result = await _wasteReportService.CancelReportAsync(actorUserId, id, request?.Note, ct);
        if (result.NotFound)
            return NotFound();

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Tracking);
    }
}
