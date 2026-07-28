using System.Text.Json.Serialization;

namespace ParseContentApi.Contracts;

public sealed class ParseContentRequest
{
    [JsonPropertyName("type")]
    public ContentFormat? Type { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }
}