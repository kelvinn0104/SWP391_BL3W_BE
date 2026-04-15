using System.ComponentModel.DataAnnotations;

namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class VoucherCode
{
    public long Id { get; set; }
    
    public long VoucherId { get; set; }
    public Voucher Voucher { get; set; } = null!;
    
    [MaxLength(100)]
    public string Code { get; set; } = null!;
    
    public bool IsUsed { get; set; } = false;
    
    public long? UsedByUserId { get; set; }
    public User? UsedByUser { get; set; }
    
    public DateTime? UsedAtUtc { get; set; }
}
