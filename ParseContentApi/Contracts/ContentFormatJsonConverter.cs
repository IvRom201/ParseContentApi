using System.Text.Json;
using System.Text.Json.Serialization;

namespace ParseContentApi.Contracts;

public sealed class ContentFormatJsonConverter : JsonConverter<ContentFormat>
{
    public override ContentFormat Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                "The content type must be represented as a string.");
        }

        var value = reader.GetString();

        return value?.ToUpperInvariant() switch
        {
            "CSV" => ContentFormat.Csv,
            "INTERNAL_JSON" => ContentFormat.InternalJson,
            _ => ContentFormat.Unknown
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        ContentFormat value,
        JsonSerializerOptions options)
    {
        var serializedValue = value switch
        {
            ContentFormat.Csv => "CSV",
            ContentFormat.InternalJson => "INTERNAL_JSON",
            _ => throw new JsonException(
                $"The content format '{value}' cannot be serialized.")
        };

        writer.WriteStringValue(serializedValue);
    }
}