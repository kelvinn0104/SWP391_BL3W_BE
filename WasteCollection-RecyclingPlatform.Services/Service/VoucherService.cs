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
    private readonly IRewardRepository _rewardRepository;

    public VoucherService(
        IVoucherRepository voucherRepository, 
        IUserRepository userRepository,
        IRewardRepository rewardRepository)
    {
        _voucherRepository = voucherRepository;
        _userRepository = userRepository;
        _rewardRepository = rewardRepository;
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
        var categoryName = string.IsNullOrWhiteSpace(request.Category) ? "Chung" : request.Category;
        var category = categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

        if (category == null)
        {
            category = new VoucherCategory { Name = categoryName };
            await _voucherRepository.AddCategoryAsync(category, ct);
        }

        var voucher = new Voucher
        {
            Title = request.Title,
            PointsRequired = request.Points,
            ImageUrl = request.Image,
            CategoryId = category.Id,
            Codes = request.Codes?.Select(c => new VoucherCode { Code = c }).ToList() ?? new List<VoucherCode>()
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
        var categoryName = string.IsNullOrWhiteSpace(request.Category) ? "Chung" : request.Category;
        var category = categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

        if (category == null)
        {
            category = new VoucherCategory { Name = categoryName };
            await _voucherRepository.AddCategoryAsync(category, ct);
        }

        voucher.Title = request.Title;
        voucher.PointsRequired = request.Points;
        voucher.CategoryId = category.Id;

        if (request.ImageFile != null)
        {
            WasteCollection_RecyclingPlatform.Services.Helpers.FileHelper.DeleteFileIfExists(voucher.ImageUrl);
            voucher.ImageUrl = await SaveVoucherImageAsync(request.ImageFile);
        }
        else if (!string.IsNullOrEmpty(request.Image))
        {
            voucher.ImageUrl = request.Image;
        }

        // Cập nhật codes
        if (request.Codes != null)
        {
            voucher.Codes.Clear();
            foreach (var code in request.Codes)
            {
                voucher.Codes.Add(new VoucherCode { Code = code });
            }
        }

        if (request.Codes != null)
        {
            // Sync codes - very basic implementation
            // Remove codes that are not used and not in the new list
            var unusedCodes = voucher.Codes.Where(c => !c.IsUsed).ToList();
            foreach (var c in unusedCodes)
            {
                if (!request.Codes.Contains(c.Code))
                {
                    voucher.Codes.Remove(c);
                }
            }

            // Add new codes
            var existingCodesStrings = voucher.Codes.Select(c => c.Code).ToList();
            foreach (var newCode in request.Codes)
            {
                if (!existingCodesStrings.Contains(newCode))
                {
                    voucher.Codes.Add(new VoucherCode { Code = newCode });
                }
            }
        }

        await _voucherRepository.UpdateVoucherAsync(voucher, ct);
        return true;
    }

    public async Task<bool> DeleteVoucherAsync(long id, CancellationToken ct = default)
    {
        var voucher = await _voucherRepository.GetVoucherByIdAsync(id, ct);
        if (voucher == null) return false;

        WasteCollection_RecyclingPlatform.Services.Helpers.FileHelper.DeleteFileIfExists(voucher.ImageUrl);
        await _voucherRepository.DeleteVoucherAsync(voucher, ct);
        return true;
    }

    public async Task<(bool Success, string? VoucherCode, string? Error)> RedeemVoucherAsync(long userId, long voucherId, CancellationToken ct = default)
    {
        using var transaction = await _rewardRepository.BeginTransactionAsync(ct);
        try
        {
            // Fetch user for update (tracked)
            var user = await _userRepository.GetByIdAsync(userId, ct);
            if (user == null) return (false, null, "Không tìm thấy người dùng.");

            var voucher = await _voucherRepository.GetVoucherByIdAsync(voucherId, ct);
            if (voucher == null) return (false, null, "Không tìm thấy Voucher.");

            // Final check of points
            if (user.Points < voucher.PointsRequired)
            {
                return (false, null, "Bạn không đủ điểm để đổi voucher này.");
            }

            var code = await _voucherRepository.GetNextAvailableCodeAsync(voucherId, ct);
            if (code == null)
            {
                return (false, null, "Voucher này hiện đã hết mã.");
            }

            // Dấu mốc bảo vệ: Trừ điểm và gán mã trong cùng một đơn vị công việc
            user.Points -= voucher.PointsRequired;
            
            code.IsUsed = true;
            code.UsedByUserId = userId;
            code.UsedAtUtc = DateTime.UtcNow;

            // Ghi nhật ký giao dịch điểm
            _rewardRepository.AddRewardPointTransaction(new RewardPointTransaction
            {
                UserId = userId,
                Amount = -voucher.PointsRequired,
                BalanceAfter = user.Points,
                TransactionType = RewardPointTransactionType.Spent,
                SourceType = RewardPointSourceType.VoucherRedemption,
                SourceRefId = voucher.Id,
                Description = $"Đổi điểm lấy voucher: {voucher.Title}",
                CreatedAtUtc = DateTime.UtcNow,
            });
            await _rewardRepository.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
            return (true, code.Code, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return (false, null, $"Lỗi hệ thống khi đổi voucher: {ex.Message}");
        }
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
        var staticFilesRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var uploadDirectory = Path.Combine(staticFilesRoot, "voucher-images");
        
        if (!Directory.Exists(uploadDirectory)) 
        {
            Directory.CreateDirectory(uploadDirectory);
        }

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadDirectory, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return "/voucher-images/" + fileName;
    }
}
