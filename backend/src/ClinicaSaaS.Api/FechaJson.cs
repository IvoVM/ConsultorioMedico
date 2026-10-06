using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClinicaSaaS.Api;

public sealed class FechaJsonConverter(TimeZoneInfo zona) : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String
            ? DateTimeOffset.Parse(reader.GetString()!)
            : reader.GetDateTimeOffset();

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(TimeZoneInfo.ConvertTime(value, zona));
}

public sealed class FechaNullableJsonConverter(TimeZoneInfo zona) : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        return reader.TokenType == JsonTokenType.String
            ? DateTimeOffset.Parse(reader.GetString()!)
            : reader.GetDateTimeOffset();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(TimeZoneInfo.ConvertTime(value.Value, zona));
    }
}
