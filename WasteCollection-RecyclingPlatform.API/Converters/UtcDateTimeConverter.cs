using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WasteCollection_RecyclingPlatform.API.Converters;

public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var date = reader.GetDateTime();
        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utcDate = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(utcDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));
    }
}

public class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.TryGetDateTime(out var date)) return null;
        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }
        var utcDate = DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
        writer.WriteStringValue(utcDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));
    }
}
