using Microsoft.AspNetCore.Mvc;
using WasteCollection_RecyclingPlatform.Services.DTOs;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AreaController : ControllerBase
{
    private readonly IAreaService _areaService;

    public AreaController(IAreaService areaService)
    {
        _areaService = areaService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AreaResponse>>> GetAll(CancellationToken ct)
    {
        var areas = await _areaService.GetAllAreasAsync(ct);
        return Ok(areas);
    }

    [HttpPut]
    public async Task<IActionResult> BulkUpdate([FromBody] AreaBulkUpdateRequest request, CancellationToken ct)
    {
        if (request?.Areas == null)
        {
            return BadRequest("Invalid request data.");
        }

        var updatedAreas = await _areaService.BulkUpdateAreasAsync(request.Areas, ct);
        return Ok(updatedAreas);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await _areaService.DeleteAreaAsync(id, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AreaResponse dto, CancellationToken ct)
    {
        var updated = await _areaService.UpdateAreaInfoAsync(id, dto, ct);
        if (!updated) return NotFound();
        return NoContent();
    }
}
