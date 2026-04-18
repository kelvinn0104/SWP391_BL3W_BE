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

    [HttpGet("my-reports")]
    public async Task<ActionResult<List<WasteReportResponse>>> GetMyReports(CancellationToken ct)
    {
        if (!_wasteReportService.TryGetCurrentUserId(User, out var citizenId))
            return Unauthorized(new { message = "Cannot identify current user." });

        return Ok(await _wasteReportService.GetCitizenReportsAsync(citizenId, ct));
    }

    [HttpGet("{id:long}/detail-report")]
    public async Task<ActionResult<WasteReportResponse>> GetDetailReport(long id, CancellationToken ct)
    {
        if (!_wasteReportService.TryGetCurrentUserId(User, out var citizenId))
            return Unauthorized(new { message = "Cannot identify current user." });

        var report = await _wasteReportService.GetCitizenReportDetailAsync(citizenId, id, ct);
        if (report is null) return NotFound();

        return Ok(report);
    }

    [HttpGet("search-report-status")]
    public async Task<ActionResult<List<WasteReportResponse>>> SearchReportsByStatus([FromQuery] WasteReportStatus status, CancellationToken ct)
    {
        if (!_wasteReportService.TryGetCurrentUserId(User, out var citizenId))
            return Unauthorized(new { message = "Cannot identify current user." });

        var reports = await _wasteReportService.SearchCitizenReportsByStatusAsync(citizenId, status, ct);
        if (reports is null)
            return BadRequest(new { message = "Invalid report status. Valid values: Pending, Accepted, Assigned, Collected, Cancelled." });

        return Ok(reports);
    }

    //Truyền mặc định status = Collected để ưu tiên hiển thị các báo cáo đã được thu gom, giúp người dùng dễ dàng theo dõi lịch sử thu gom của mình.
    [HttpGet("report-collected-status")]
    public async Task<ActionResult<List<WasteReportResponse>>> SearchCollectedReports(
        [FromQuery] WasteReportStatus status = WasteReportStatus.Collected,
        CancellationToken ct = default)
    {
        if (!_wasteReportService.TryGetCurrentUserId(User, out var citizenId))
            return Unauthorized(new { message = "Cannot identify current user." });

        var reports = await _wasteReportService.SearchCitizenReportsByStatusAsync(citizenId, status, ct);
        if (reports is null)
            return BadRequest(new { message = "Invalid report status. Valid values: Pending, Accepted, Assigned, Collected, Cancelled." });

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
            return Unauthorized(new { message = "Cannot identify current user." });

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
            return Unauthorized(new { message = "Cannot identify current user." });

        var result = await _wasteReportService.UpdateReportAsync(citizenId, id, request, ct);
        if (result.NotFound)
            return NotFound();

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Report);
    }

    [HttpPost("{id:long}/advance-status")]
    [Authorize(Roles = "Administrator,RecyclingEnterprise,Collector")]
    public async Task<ActionResult<WasteReportStatusTrackingResponse>> AdvanceStatus(long id, [FromBody] WasteReportStatusActionRequest? request, CancellationToken ct)
    {
        if (!_wasteReportService.TryGetCurrentUserId(User, out var actorUserId))
            return Unauthorized(new { message = "Cannot identify current user." });

        var result = await _wasteReportService.AdvanceReportStatusAsync(actorUserId, id, request?.Note, ct);
        if (result.NotFound)
            return NotFound();

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Tracking);
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<ActionResult<WasteReportStatusTrackingResponse>> CancelReport(long id, [FromBody] WasteReportStatusActionRequest? request, CancellationToken ct)
    {
        if (!_wasteReportService.TryGetCurrentUserId(User, out var actorUserId))
            return Unauthorized(new { message = "Cannot identify current user." });

        var result = await _wasteReportService.CancelReportAsync(actorUserId, id, request?.Note, ct);
        if (result.NotFound)
            return NotFound();

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Tracking);
    }
}
