using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Services.Model;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/requests")]
public class CollectionController : ControllerBase
{
    private readonly ICollectionService _collectionService;

    public CollectionController(ICollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    [HttpGet]
    [Authorize(Roles = "Administrator,RecyclingEnterprise")]
    public async Task<ActionResult<List<CollectionRequestResponse>>> GetAllRequests(CancellationToken ct)
    {
        var requests = await _collectionService.GetAllRequestsAsync(ct);
        return Ok(requests);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CollectionRequestResponse>> GetRequestById(long id, CancellationToken ct)
    {
        var request = await _collectionService.GetRequestByIdAsync(id, ct);
        if (request == null) return NotFound();
        return Ok(request);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Administrator,RecyclingEnterprise,Collector")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateCollectionStatusRequest request, CancellationToken ct)
    {
        var success = await _collectionService.UpdateStatusAsync(id, request.Status, request.CancellationReason, ct);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPatch("{id}/assign")]
    [Authorize(Roles = "Administrator,RecyclingEnterprise")]
    public async Task<IActionResult> AssignCollector(long id, [FromBody] AssignCollectorRequest request, CancellationToken ct)
    {
        var success = await _collectionService.AssignCollectorAsync(id, request.CollectorId, ct);
        if (!success) return BadRequest("Could not assign collector. Ensure collector exists and request is valid.");
        return NoContent();
    }

    [HttpGet("my-requests")]
    public async Task<ActionResult<List<CollectionRequestResponse>>> GetMyRequests(CancellationToken ct)
    {
        // Get user ID from claims (mocking current user for simplicity in this context)
        // In a real app, use User.FindFirstValue(ClaimTypes.NameIdentifier)
        var userIdStr = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            return Unauthorized();

        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (role == UserRole.Citizen.ToString() || role == "1")
            return Ok(await _collectionService.GetCitizenRequestsAsync(userId, ct));
        
        if (role == UserRole.Collector.ToString() || role == "4")
            return Ok(await _collectionService.GetCollectorRequestsAsync(userId, ct));

        return Ok(new List<CollectionRequestResponse>());
    }
}
