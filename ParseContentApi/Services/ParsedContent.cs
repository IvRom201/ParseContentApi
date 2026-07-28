using System.Text.Json.Nodes;

namespace ParseContentApi.Services;

public sealed record ParsedContent(
    IReadOnlyList<JsonObject> Records);