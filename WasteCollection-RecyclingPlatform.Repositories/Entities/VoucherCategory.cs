using System.ComponentModel.DataAnnotations;

namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class VoucherCategory
{
    public int Id { get; set; }
    
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    public ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
