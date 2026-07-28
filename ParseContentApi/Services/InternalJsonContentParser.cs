using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using ParseContentApi.Contracts;
using ParseContentApi.Exceptions;
using ParseContentApi.Options;

namespace ParseContentApi.Services;

public sealed class InternalJsonContentParser : IContentParser
{
    private readonly ContentParsingOptions _options;

    public InternalJsonContentParser(
        IOptions<ContentParsingOptions> options)
    {
        _options = options.Value;
    }

    public ContentFormat SupportedFormat => ContentFormat.InternalJson;

    public ParsedContent Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        JsonNode? rootNode;

        try
        {
            rootNode = JsonNode.Parse(
                content,
                nodeOptions: null,
                documentOptions: new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = _options.MaxJsonDepth
                });
        }
        catch (JsonException exception)
        {
            throw new ContentParsingException(
                "The decoded INTERNAL_JSON content is not valid JSON.",
                exception);
        }

        if (rootNode is JsonArray array)
        {
            return ParseArray(array);
        }

        var singleRecord = ConvertToRecord(rootNode);

        return new ParsedContent(
            new List<JsonObject>
            {
                singleRecord
            });
    }

    private ParsedContent ParseArray(JsonArray array)
    {
        if (array.Count > _options.MaxRecords)
        {
            throw new PayloadTooLargeException(
                $"The JSON array exceeds the maximum number of " +
                $"{_options.MaxRecords} records.");
        }

        var records = new List<JsonObject>(array.Count);

        foreach (var element in array)
        {
            records.Add(ConvertToRecord(element));
        }

        return new ParsedContent(records);
    }

    private static JsonObject ConvertToRecord(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            return jsonObject.DeepClone().AsObject();
        }

        return new JsonObject
        {
            ["value"] = node?.DeepClone()
        };
    }
}