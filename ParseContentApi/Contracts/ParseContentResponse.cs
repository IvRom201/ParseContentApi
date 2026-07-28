using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ParseContentApi.Contracts;

public sealed record ParseContentResponse(
    [property: JsonPropertyName("status")]
    string Status,

    [property: JsonPropertyName("type")]
    ContentFormat Type,

    [property: JsonPropertyName("processedCount")]
    int ProcessedCount,

    [property: JsonPropertyName("data")]
    IReadOnlyList<JsonObject> Data);