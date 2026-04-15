using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Entities;
using WasteCollection_RecyclingPlatform.Repositories.Repository;
using WasteCollection_RecyclingPlatform.Services.DTOs;

namespace WasteCollection_RecyclingPlatform.Services.Service;

public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _voucherRepository;
    private readonly IUserRepository _userRepository;

    public VoucherService(IVoucherRepository voucherRepository, IUserRepository userRepository)
    {
        _voucherRepository = voucherRepository;
        _userRepository = userRepository;
    }

    public async Task<List<VoucherCategoryResponse>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _voucherRepository.GetCategoriesAsync(ct);
        return categories.Select(c => new VoucherCategoryResponse
        {
            Id = c.Id,
            Name = c.Name,
            VoucherCount = c.Vouchers.Count
        }).ToList();
    }

    public async Task<bool> AddCategoryAsync(CategoryCreateRequest request, CancellationToken ct = default)
    {
        var category = new VoucherCategory { Name = request.Name };
        await _voucherRepository.AddCategoryAsync(category, ct);
        return true;
    }

    public async Task<bool> UpdateCategoryAsync(int id, CategoryCreateRequest request, CancellationToken ct = default)
    {
        var category = await _voucherRepository.GetCategoryByIdAsync(id, ct);
        if (category == null) return false;

        category.Name = request.Name;
        await _voucherRepository.UpdateCategoryAsync(category, ct);
        return true;
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken ct = default)
    {
        var category = await _voucherRepository.GetCategoryByIdAsync(id, ct);
        if (category == null) return false;

        await _voucherRepository.DeleteCategoryAsync(category, ct);
        return true;
    }

    public async Task<List<VoucherResponse>> GetAllVouchersAsync(CancellationToken ct = default)
    {
        var vouchers = await _voucherRepository.GetVouchersAsync(ct);
        return vouchers.Select(v => new VoucherResponse
        {
            Id = v.Id,
            Title = v.Title,
            Points = v.PointsRequired,
            Category = v.Category?.Name ?? "N/A",
            Stock = v.Codes.Count(c => !c.IsUsed),
            Image = v.ImageUrl,
            Codes = v.Codes.Where(c => !c.IsUsed).Select(c => c.Code).ToList()
        }).ToList();
    }

    public async Task<VoucherResponse?> GetVoucherByIdAsync(long id, CancellationToken ct = default)
    {
        var v = await _voucherRepository.GetVoucherByIdAsync(id, ct);
        if (v == null) return null;

        return new VoucherResponse
        {
            Id = v.Id,
            Title = v.Title,
            Points = v.PointsRequired,
            Category = v.Category?.Name ?? "N/A",
            Stock = v.Codes.Count(c => !c.IsUsed),
            Image = v.ImageUrl,
            Codes = v.Codes.Where(c => !c.IsUsed).Select(c => c.Code).ToList()
        };
    }

    public async Task<bool> CreateVoucherAsync(VoucherCreateRequest request, CancellationToken ct = default)
    {
        var categories = await _voucherRepository.GetCategoriesAsync(ct);
        var category = categories.FirstOrDefault(c => c.Name.Equals(request.Category, StringComparison.OrdinalIgnoreCase));

        if (category == null)
        {
            category = new VoucherCategory { Name = request.Category };
            await _voucherRepository.AddCategoryAsync(category, ct);
        }

        var voucher = new Voucher
        {
            Title = request.Title,
            PointsRequired = request.Points,
            ImageUrl = request.Image,
            CategoryId = category.Id,
            Codes = request.Codes.Select(c => new VoucherCode { Code = c }).ToList()
        };

        if (request.ImageFile != null)
        {
            voucher.ImageUrl = await SaveVoucherImageAsync(request.ImageFile);
        }

        await _voucherRepository.AddVoucherAsync(voucher, ct);
        return true;
    }

    public async Task<bool> UpdateVoucherAsync(long id, VoucherUpdateRequest request, CancellationToken ct = default)
    {
        var voucher = await _voucherRepository.GetVoucherByIdAsync(id, ct);
        if (voucher == null) return false;

        var categories = await _voucherRepository.GetCategoriesAsync(ct);
        var category = categories.FirstOrDefault(c => c.Name.Equals(request.Category, StringComparison.OrdinalIgnoreCase));

        if (category == null)
        {
            category = new VoucherCategory { Name = request.Category };
            await _voucherRepository.AddCategoryAsync(category, ct);
        }

        voucher.Title = request.Title;
        voucher.PointsRequired = request.Points;
        voucher.CategoryId = category.Id;

        if (request.ImageFile != null)
        {
            voucher.ImageUrl = await SaveVoucherImageAsync(request.ImageFile);
        }
        else if (!string.IsNullOrEmpty(request.Image))
        {
            voucher.ImageUrl = request.Image;
        }

        // Sync codes - very basic implementation
        // Remove codes that are not used and not in the new list
        var unusedCodes = voucher.Codes.Where(c => !c.IsUsed).ToList();
        foreach(var c in unusedCodes)
        {
            if (!request.Codes.Contains(c.Code))
            {
                voucher.Codes.Remove(c);
            }
        }

        // Add new codes
        var existingCodesStrings = voucher.Codes.Select(c => c.Code).ToList();
        foreach(var newCode in request.Codes)
        {
            if (!existingCodesStrings.Contains(newCode))
            {
                voucher.Codes.Add(new VoucherCode { Code = newCode });
            }
        }

        await _voucherRepository.UpdateVoucherAsync(voucher, ct);
        return true;
    }

    public async Task<bool> DeleteVoucherAsync(long id, CancellationToken ct = default)
    {
        var voucher = await _voucherRepository.GetVoucherByIdAsync(id, ct);
        if (voucher == null) return false;

        await _voucherRepository.DeleteVoucherAsync(voucher, ct);
        return true;
    }

    public async Task<(bool Success, string? VoucherCode, string? Error)> RedeemVoucherAsync(long userId, long voucherId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null) return (false, null, "User not found");

        var voucher = await _voucherRepository.GetVoucherByIdAsync(voucherId, ct);
        if (voucher == null) return (false, null, "Voucher not found");

        if (user.Points < voucher.PointsRequired)
        {
            return (false, null, "Insufficient points");
        }

        var code = await _voucherRepository.GetNextAvailableCodeAsync(voucherId, ct);
        if (code == null)
        {
            return (false, null, "Voucher is out of stock");
        }

        // Action
        user.Points -= voucher.PointsRequired;
        await _userRepository.UpdateAsync(user, ct);

        code.IsUsed = true;
        code.UsedByUserId = userId;
        code.UsedAtUtc = DateTime.UtcNow;
        await _voucherRepository.UpdateVoucherCodeAsync(code, ct);

        return (true, code.Code, null);
    }

    public async Task<List<VoucherHistoryResponse>> GetRedemptionHistoryAsync(long? userId = null, CancellationToken ct = default)
    {
        var history = await _voucherRepository.GetRedemptionHistoryAsync(userId, ct);
        return history.Select(h => new VoucherHistoryResponse
        {
            Id = h.Id,
            User = h.UsedByUser?.DisplayName ?? h.UsedByUser?.Email ?? "N/A",
            Gift = h.Voucher?.Title ?? "N/A",
            Points = h.Voucher?.PointsRequired ?? 0,
            Date = h.UsedAtUtc?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
            CodeUsed = h.Code,
            TransactionId = $"TXN{h.Id}{h.UsedAtUtc?.Ticks % 1000}"
        }).ToList();
    }

    private async Task<string> SaveVoucherImageAsync(IFormFile file)
    {
        // Target: WasteCollection-RecyclingPlatform.FE/public/voucher
        // Path is hardcoded based on user's project structure
        var fePublicPath = @"d:\WasteCollection-RecyclingPlatform\WasteCollection-RecyclingPlatform.FE\public\voucher";
        
        if (!Directory.Exists(fePublicPath))
        {
            Directory.CreateDirectory(fePublicPath);
        }

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(fePublicPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return path relative to FE root
        return $"/voucher/{fileName}";
    }
}
