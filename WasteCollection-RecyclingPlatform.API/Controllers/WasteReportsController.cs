using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WasteCollection_RecyclingPlatform.Services.DTOs;
using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/waste-reports")]
public class WasteReportsController : ControllerBase
{
    private readonly IWasteReportService _wasteReportService;

    public WasteReportsController(IWasteReportService wasteReportService)
    {
        _wasteReportService = wasteReportService;
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<List<WasteCategoryResponse>>> GetCategories(CancellationToken ct)
    {
        return Ok(await _wasteReportService.GetCategoriesAsync(ct));
    }

    [HttpGet("my-reports")]
    public async Task<ActionResult<List<WasteReportResponse>>> GetMyReports(CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var citizenId))
            return Unauthorized(new { message = "Không xác định được người dùng hiện tại." });

        return Ok(await _wasteReportService.GetCitizenReportsAsync(citizenId, ct));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<WasteReportResponse>> CreateReport([FromForm] WasteReportCreateRequest request, CancellationToken ct)
    {
        var formItemsResult = TryBindWasteItemsFromRawForm(request);
        if (!formItemsResult.Success)
            return BadRequest(new { message = formItemsResult.Error });

        if (!TryGetCurrentUserId(out var citizenId))
            return Unauthorized(new { message = "Không xác định được người dùng hiện tại." });

        var result = await _wasteReportService.CreateReportAsync(citizenId, request, ct);
        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return StatusCode(StatusCodes.Status201Created, result.Report);
    }

    private (bool Success, string? Error) TryBindWasteItemsFromRawForm(WasteReportCreateRequest request)
    {
        if (!Request.HasFormContentType)
            return (true, null);

        var form = Request.Form;
        if (request.WasteCategoryIds.Count == 0)
        {
            BindPrimitiveListFromForm(form, "WasteCategoryIds", request.WasteCategoryIds);
            BindPrimitiveListFromForm(form, "wasteCategoryIds", request.WasteCategoryIds);
            BindIndexedPrimitiveListFromForm(form, "WasteCategoryIds", request.WasteCategoryIds);
            BindIndexedPrimitiveListFromForm(form, "wasteCategoryIds", request.WasteCategoryIds);
        }

        if (request.EstimatedWeightKgs.Count == 0)
        {
            BindPrimitiveListFromForm(form, "EstimatedWeightKgs", request.EstimatedWeightKgs);
            BindPrimitiveListFromForm(form, "estimatedWeightKgs", request.EstimatedWeightKgs);
            BindIndexedPrimitiveListFromForm(form, "EstimatedWeightKgs", request.EstimatedWeightKgs);
            BindIndexedPrimitiveListFromForm(form, "estimatedWeightKgs", request.EstimatedWeightKgs);
        }

        if (request.WasteCategoryIds.Count > 0)
            return (true, null);

        foreach (var rawWasteItems in form["WasteItems"].Concat(form["wasteItems"]))
        {
            if (string.IsNullOrWhiteSpace(rawWasteItems)) continue;

            try
            {
                using var document = JsonDocument.Parse(rawWasteItems);
                BindRawWasteItems(request, document.RootElement);

                if (request.WasteCategoryIds.Count > 0)
                    return (true, null);
            }
            catch
            {
                // Legacy WasteItems is only a compatibility fallback.
            }
        }

        return (true, null);
    }

    private static void BindRawWasteItems(WasteReportCreateRequest request, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var itemElement in element.EnumerateArray())
            {
                BindRawWasteItems(request, itemElement);
            }

            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return;

        if (TryGetJsonProperty(element, "wasteItems", out var wasteItemsElement))
        {
            BindRawWasteItems(request, wasteItemsElement);
            return;
        }

        if (!TryGetJsonProperty(element, "wasteCategoryId", out var categoryIdElement)
            || !TryGetLong(categoryIdElement, out var categoryId))
        {
            return;
        }

        request.WasteCategoryIds.Add(categoryId);

        if (TryGetJsonProperty(element, "estimatedWeightKg", out var weightElement)
            && TryGetDecimal(weightElement, out var estimatedWeightKg))
        {
            request.EstimatedWeightKgs.Add(estimatedWeightKg);
        }
        else
        {
            request.EstimatedWeightKgs.Add(null);
        }
    }

    private static void BindPrimitiveListFromForm<T>(IFormCollection form, string key, List<T> target)
    {
        foreach (var rawValue in form[key])
        {
            AddPrimitiveValues(rawValue, target);
        }
    }

    private static void BindIndexedPrimitiveListFromForm<T>(IFormCollection form, string key, List<T> target)
    {
        var prefix = key + "[";
        var values = form.Keys
            .Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(x => new
            {
                Key = x,
                Index = TryReadIndex(x, prefix.Length, out var index) ? index : int.MaxValue,
            })
            .OrderBy(x => x.Index)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .SelectMany(x => form[x.Key]);

        foreach (var rawValue in values)
        {
            AddPrimitiveValues(rawValue, target);
        }
    }

    private static void AddPrimitiveValues<T>(string? rawValue, List<T> target)
    {
        if (string.IsNullOrWhiteSpace(rawValue)) return;

        try
        {
            if (rawValue.TrimStart().StartsWith("[", StringComparison.Ordinal))
            {
                var values = JsonSerializer.Deserialize<List<T>>(rawValue, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (values is not null) target.AddRange(values);
                return;
            }

            foreach (var value in rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                target.Add((T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)));
            }
        }
        catch
        {
            // Business validation will report missing/invalid categories.
        }
    }

    private static bool TryGetJsonProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetLong(JsonElement element, out long value)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt64(out value);

        if (element.ValueKind == JsonValueKind.String)
            return long.TryParse(element.GetString(), out value);

        value = default;
        return false;
    }

    private static bool TryGetDecimal(JsonElement element, out decimal value)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetDecimal(out value);

        if (element.ValueKind == JsonValueKind.String)
            return decimal.TryParse(element.GetString(), out value);

        value = default;
        return false;
    }

    private static bool TryReadIndex(string key, int startIndex, out int index)
    {
        var endIndex = key.IndexOf(']', startIndex);
        if (endIndex <= startIndex)
        {
            index = default;
            return false;
        }

        return int.TryParse(key[startIndex..endIndex], out index);
    }

    private bool TryGetCurrentUserId(out long userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("id");

        return long.TryParse(raw, out userId);
    }
}
