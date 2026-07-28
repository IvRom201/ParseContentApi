using ParseContentApi.Exceptions;
using ParseContentApi.Options;
using ParseContentApi.Services;

namespace ParseContentApi.Tests.Unit;

public sealed class InternalJsonContentParserTests
{
    [Fact]
    public void Parse_ArrayOfObjects_ReturnsRecords()
    {
        var parser = CreateParser();

        const string json =
            """
            [
              {
                "id": 1,
                "name": "Alice"
              },
              {
                "id": 2,
                "name": "Bob"
              }
            ]
            """;

        var result = parser.Parse(json);

        Assert.Equal(2, result.Records.Count);

        Assert.Equal(
            1,
            result.Records[0]["id"]!.GetValue<int>());

        Assert.Equal(
            "Alice",
            result.Records[0]["name"]!.GetValue<string>());

        Assert.Equal(
            2,
            result.Records[1]["id"]!.GetValue<int>());

        Assert.Equal(
            "Bob",
            result.Records[1]["name"]!.GetValue<string>());
    }

    [Fact]
    public void Parse_SingleObject_ReturnsSingleRecord()
    {
        var parser = CreateParser();

        const string json =
            """
            {
              "id": 42,
              "active": true
            }
            """;

        var result = parser.Parse(json);

        Assert.Single(result.Records);

        Assert.Equal(
            42,
            result.Records[0]["id"]!.GetValue<int>());

        Assert.True(
            result.Records[0]["active"]!.GetValue<bool>());
    }

    [Fact]
    public void Parse_StringRoot_WrapsValueInObject()
    {
        var parser = CreateParser();

        const string json = "\"Hello\"";

        var result = parser.Parse(json);

        Assert.Single(result.Records);

        Assert.Equal(
            "Hello",
            result.Records[0]["value"]!.GetValue<string>());
    }

    [Fact]
    public void Parse_NumberInsideArray_WrapsValueInObject()
    {
        var parser = CreateParser();

        const string json = "[10, 20]";

        var result = parser.Parse(json);

        Assert.Equal(2, result.Records.Count);

        Assert.Equal(
            10,
            result.Records[0]["value"]!.GetValue<int>());

        Assert.Equal(
            20,
            result.Records[1]["value"]!.GetValue<int>());
    }

    [Fact]
    public void Parse_NullInsideArray_ReturnsObjectWithNullValue()
    {
        var parser = CreateParser();

        const string json = "[null]";

        var result = parser.Parse(json);

        Assert.Single(result.Records);
        Assert.Null(result.Records[0]["value"]);
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsContentParsingException()
    {
        var parser = CreateParser();

        const string invalidJson =
            """
            {
              "id": 1,
            }
            """;

        var exception = Assert.Throws<ContentParsingException>(
            () => parser.Parse(invalidJson));

        Assert.Contains(
            "not valid JSON",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_ArrayExceedsRecordLimit_ThrowsPayloadTooLargeException()
    {
        var parser = CreateParser(maxRecords: 1);

        const string json =
            """
            [
              { "id": 1 },
              { "id": 2 }
            ]
            """;

        Assert.Throws<PayloadTooLargeException>(
            () => parser.Parse(json));
    }

    [Fact]
    public void Parse_JsonExceedsDepthLimit_ThrowsContentParsingException()
    {
        var parser = CreateParser(maxJsonDepth: 2);

        const string json =
            """
            {
              "level1": {
                "level2": {
                  "value": 1
                }
              }
            }
            """;

        Assert.Throws<ContentParsingException>(
            () => parser.Parse(json));
    }

    private static InternalJsonContentParser CreateParser(
        int maxRecords = 100,
        int maxJsonDepth = 64)
    {
        var options = OptionsFactory.Create(
            new ContentParsingOptions
            {
                MaxRecords = maxRecords,
                MaxJsonDepth = maxJsonDepth
            });

        return new InternalJsonContentParser(options);
    }
}