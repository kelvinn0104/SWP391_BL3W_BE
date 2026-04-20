using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace WasteCollection_RecyclingPlatform.Services.DTOs;

public class VoucherCategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int VoucherCount { get; set; }
}

public class VoucherResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public int Points { get; set; }
    public string Category { get; set; } = null!;
    public int Stock { get; set; }
    public string? Image { get; set; }
    public List<string> Codes { get; set; } = new List<string>();
}

public class VoucherCreateRequest
{
    public string Title { get; set; } = null!;
    public int Points { get; set; }
    public string Category { get; set; } = null!;
    public int Stock { get; set; }
    public string? Image { get; set; }
    public IFormFile? ImageFile { get; set; }
    public List<string> Codes { get; set; } = new List<string>();
}

public class VoucherUpdateRequest
{
    public string Title { get; set; } = null!;
    public int Points { get; set; }
    public string Category { get; set; } = null!;
    public int Stock { get; set; }
    public string? Image { get; set; }
    public IFormFile? ImageFile { get; set; }
    public List<string> Codes { get; set; } = new List<string>();
}

public class VoucherHistoryResponse
{
    public long Id { get; set; }
    public string User { get; set; } = null!;
    public string Gift { get; set; } = null!;
    public int Points { get; set; }
    public string Date { get; set; } = null!;
    public string Status { get; set; } = "approved";
    public string CodeUsed { get; set; } = null!;
    public string TransactionId { get; set; } = null!;
}

public class CategoryCreateRequest
{
    public string Name { get; set; } = null!;
}
