using System.ComponentModel.DataAnnotations;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Services.Model;

public record CollectionRequestResponse(
    long Id,
    long CitizenId,
    string CitizenName,
    long? CollectorId,
    string? CollectorName,
    string? CollectorPhone,
    string Address,
    string WasteType,
    decimal WeightKg,
    string? Note,
    string? Priority,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? CancellationReason,
    long? WardId,
    string? WardName,
    List<WasteMaterialDto> Materials,
    List<string> Images
);

public record WasteMaterialDto(
    string Type,
    decimal Amount,
    string Unit
);

public record UpdateCollectionStatusRequest(
    [Required] CollectionRequestStatus Status,
    string? CancellationReason
);

public record AssignCollectorRequest(
    [Required] long CollectorId
);
