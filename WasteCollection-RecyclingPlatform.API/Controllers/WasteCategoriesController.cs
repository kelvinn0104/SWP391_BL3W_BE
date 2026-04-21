using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WasteCollection_RecyclingPlatform.Services.DTOs;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Controllers;

[ApiController]
[Route("api/waste-categories")]
public class WasteCategoriesController : ControllerBase
{
    private readonly IWasteCategoryService _service;

    public WasteCategoriesController(IWasteCategoryService service)
    {
        _service = service;
    }

    /// <summary>Lấy toàn bộ danh mục rác (public, dùng cho FE citizen và admin)</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<WasteCategoryDetailResponse>>> GetAll(CancellationToken ct)
    {
        return Ok(await _service.GetAllAsync(ct));
    }

    /// <summary>Lấy chi tiết 1 danh mục</summary>
    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<WasteCategoryDetailResponse>> GetById(long id, CancellationToken ct)
    {
        var cat = await _service.GetByIdAsync(id, ct);
        if (cat is null) return NotFound(new { message = "Không tìm thấy danh mục." });
        return Ok(cat);
    }

    /// <summary>Tạo danh mục mới (chỉ Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<WasteCategoryDetailResponse>> Create(
        [FromBody] WasteCategoryCreateRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return CreatedAtAction(nameof(GetById), new { id = result.Category!.Id }, result.Category);
    }

    /// <summary>Cập nhật tỷ lệ điểm (PointsPerKg) cho danh mục (chỉ Admin)</summary>
    [HttpPatch("{id:long}/points")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<WasteCategoryDetailResponse>> UpdatePoints(
        long id, [FromBody] WasteCategoryUpdatePointsRequest request, CancellationToken ct)
    {
        var result = await _service.UpdatePointsAsync(id, request, ct);
        if (result.NotFound) return NotFound(new { message = result.Error });
        if (!result.Success) return BadRequest(new { message = result.Error });
        return Ok(result.Category);
    }

    /// <summary>Bật/tắt trạng thái hoạt động của danh mục (chỉ Admin)</summary>
    [HttpPatch("{id:long}/toggle-active")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<WasteCategoryDetailResponse>> ToggleActive(long id, CancellationToken ct)
    {
        var result = await _service.ToggleActiveAsync(id, ct);
        if (result.NotFound) return NotFound(new { message = result.Error });
        return Ok(result.Category);
    }
}
