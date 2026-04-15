using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WasteCollection_RecyclingPlatform.Services.DTOs;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VouchersController : ControllerBase
{
    private readonly IVoucherService _voucherService;

    public VouchersController(IVoucherService voucherService)
    {
        _voucherService = voucherService;
    }

    [HttpGet]
    public async Task<ActionResult<List<VoucherResponse>>> GetVouchers(CancellationToken ct)
    {
        return Ok(await _voucherService.GetAllVouchersAsync(ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VoucherResponse>> GetVoucher(long id, CancellationToken ct)
    {
        var voucher = await _voucherService.GetVoucherByIdAsync(id, ct);
        if (voucher == null) return NotFound();
        return Ok(voucher);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    // [Authorize(Roles = "Admin,Enterprise")]
    public async Task<IActionResult> CreateVoucher([FromForm] VoucherCreateRequest request, CancellationToken ct)
    {
        await _voucherService.CreateVoucherAsync(request, ct);
        return Ok();
    }

    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    // [Authorize(Roles = "Admin,Enterprise")]
    public async Task<IActionResult> UpdateVoucher(long id, [FromForm] VoucherUpdateRequest request, CancellationToken ct)
    {
        var success = await _voucherService.UpdateVoucherAsync(id, request, ct);
        if (!success) return NotFound();
        return Ok();
    }


    [HttpDelete("{id}")]
    // [Authorize(Roles = "Admin,Enterprise")]
    public async Task<IActionResult> DeleteVoucher(long id, CancellationToken ct)
    {
        var deleted = await _voucherService.DeleteVoucherAsync(id, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<VoucherCategoryResponse>>> GetCategories(CancellationToken ct)
    {
        return Ok(await _voucherService.GetCategoriesAsync(ct));
    }

    [HttpPost("categories")]
    // [Authorize(Roles = "Admin,Enterprise")]
    public async Task<IActionResult> AddCategory([FromBody] CategoryCreateRequest request, CancellationToken ct)
    {
        await _voucherService.AddCategoryAsync(request, ct);
        return Ok();
    }

    [HttpPut("categories/{id}")]
    // [Authorize(Roles = "Admin,Enterprise")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryCreateRequest request, CancellationToken ct)
    {
        var success = await _voucherService.UpdateCategoryAsync(id, request, ct);
        if (!success) return NotFound();
        return Ok();
    }

    [HttpDelete("categories/{id}")]
    // [Authorize(Roles = "Admin,Enterprise")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
    {
        var success = await _voucherService.DeleteCategoryAsync(id, ct);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPost("redeem/{voucherId}")]
    [Authorize]
    public async Task<IActionResult> Redeem(long voucherId, CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!long.TryParse(sub, out var userId))
            return Unauthorized();

        var (success, voucherCode, error) = await _voucherService.RedeemVoucherAsync(userId, voucherId, ct);
        if (!success)
        {
            return BadRequest(new { message = error });
        }

        return Ok(new { code = voucherCode });
    }

    [HttpGet("history")]
    // [Authorize]
    public async Task<ActionResult<List<VoucherHistoryResponse>>> GetHistory(CancellationToken ct, [FromQuery] long? userId = null)
    {
        // If not admin, force userId to be current user
        // For simplicity, returning all if no userId provided
        return Ok(await _voucherService.GetRedemptionHistoryAsync(userId, ct));
    }
}
