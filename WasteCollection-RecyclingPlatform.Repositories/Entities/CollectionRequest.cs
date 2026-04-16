using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class CollectionRequest
{
    public long Id { get; set; }

    [Required]
    public long CitizenId { get; set; }
    public User Citizen { get; set; } = null!;

    public long? CollectorId { get; set; }
    public User? Collector { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string WasteType { get; set; } = string.Empty;

    public decimal WeightKg { get; set; }

    public string? Note { get; set; }

    public string? Priority { get; set; } // High, Medium, Standard

    [Required]
    public CollectionRequestStatus Status { get; set; } = CollectionRequestStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }

    public string? CancellationReason { get; set; }

    // Stored as JSON strings
    public string? MaterialsJson { get; set; }
    public string? ImagesJson { get; set; }
 
    public long? WardId { get; set; }
    public Ward? Ward { get; set; }

    public string? CitizenName { get; set; }
    public string? CollectorName { get; set; }
    public string? CollectorPhone { get; set; }
}
