using System.ComponentModel.DataAnnotations;

namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class Voucher
{
    public long Id { get; set; }
    
    [MaxLength(255)]
    public string Title { get; set; } = null!;
    
    public int PointsRequired { get; set; }
    
    [MaxLength(1000)]
    public string? ImageUrl { get; set; }
    
    public int CategoryId { get; set; }
    public VoucherCategory Category { get; set; } = null!;
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<VoucherCode> Codes { get; set; } = new List<VoucherCode>();
}
