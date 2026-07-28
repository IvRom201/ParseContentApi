using ParseContentApi.Exceptions;
using ParseContentApi.Options;
using ParseContentApi.Services;

namespace ParseContentApi.Tests.Unit;

public sealed class CsvContentParserTests
{
    [Fact]
    public void Parse_ValidCsv_ReturnsRecords()
    {
        var parser = CreateParser();

        const string csv =
            """
            name,age
            Alice,30
            Bob,28
            """;

        var result = parser.Parse(csv);

        Assert.Equal(2, result.Records.Count);

        Assert.Equal(
            "Alice",
            result.Records[0]["name"]!.GetValue<string>());

        Assert.Equal(
            "30",
            result.Records[0]["age"]!.GetValue<string>());

        Assert.Equal(
            "Bob",
            result.Records[1]["name"]!.GetValue<string>());

        Assert.Equal(
            "28",
            result.Records[1]["age"]!.GetValue<string>());
    }

    [Fact]
    public void Parse_QuotedComma_ParsesFieldAsSingleValue()
    {
        var parser = CreateParser();

        const string csv =
            """
            name,description
            Alice,"Hello, world"
            """;

        var result = parser.Parse(csv);

        Assert.Single(result.Records);

        Assert.Equal(
            "Hello, world",
            result.Records[0]["description"]!.GetValue<string>());
    }

    [Fact]
    public void Parse_EscapedQuote_ParsesQuoteInsideField()
    {
        var parser = CreateParser();

        const string csv =
            "name,description\n" +
            "Alice,\"She said \"\"Hello\"\"\"";

        var result = parser.Parse(csv);

        Assert.Single(result.Records);

        Assert.Equal(
            "She said \"Hello\"",
            result.Records[0]["description"]!.GetValue<string>());
    }

    [Fact]
    public void Parse_NewLineInsideQuotedField_PreservesNewLine()
    {
        var parser = CreateParser();

        const string csv =
            "name,description\n" +
            "Alice,\"First line\nSecond line\"";

        var result = parser.Parse(csv);

        Assert.Single(result.Records);

        Assert.Equal(
            "First line\nSecond line",
            result.Records[0]["description"]!.GetValue<string>());
    }

    [Fact]
    public void Parse_DuplicateHeaders_ThrowsContentParsingException()
    {
        var parser = CreateParser();

        const string csv =
            """
            name,NAME
            Alice,Bob
            """;

        var exception = Assert.Throws<ContentParsingException>(
            () => parser.Parse(csv));

        Assert.Contains(
            "duplicated",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_EmptyHeader_ThrowsContentParsingException()
    {
        var parser = CreateParser();

        const string csv =
            """
            name,,age
            Alice,test,30
            """;

        var exception = Assert.Throws<ContentParsingException>(
            () => parser.Parse(csv));

        Assert.Contains(
            "header",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_InconsistentColumnCount_ThrowsContentParsingException()
    {
        var parser = CreateParser();

        const string csv =
            """
            name,age
            Alice,30
            Bob
            """;

        var exception = Assert.Throws<ContentParsingException>(
            () => parser.Parse(csv));

        Assert.Contains(
            "contains 1 fields",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_UnclosedQuotedField_ThrowsContentParsingException()
    {
        var parser = CreateParser();

        const string csv =
            """
            name,description
            Alice,"Unclosed value
            """;

        var exception = Assert.Throws<ContentParsingException>(
            () => parser.Parse(csv));

        Assert.Contains(
            "unclosed",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_RecordLimitExceeded_ThrowsPayloadTooLargeException()
    {
        var parser = CreateParser(maxRecords: 1);

        const string csv =
            """
            name
            Alice
            Bob
            """;

        Assert.Throws<PayloadTooLargeException>(
            () => parser.Parse(csv));
    }

    [Fact]
    public void Parse_ColumnLimitExceeded_ThrowsPayloadTooLargeException()
    {
        var parser = CreateParser(maxCsvColumns: 2);

        const string csv =
            """
            first,second,third
            one,two,three
            """;

        Assert.Throws<PayloadTooLargeException>(
            () => parser.Parse(csv));
    }

    [Fact]
    public void Parse_FieldLengthExceeded_ThrowsPayloadTooLargeException()
    {
        var parser = CreateParser(maxFieldLength: 3);

        const string csv =
            """
            name
            Alice
            """;

        Assert.Throws<PayloadTooLargeException>(
            () => parser.Parse(csv));
    }

    private static CsvContentParser CreateParser(
        int maxRecords = 100,
        int maxCsvColumns = 100,
        int maxFieldLength = 10_000)
    {
        var options = OptionsFactory.Create(
            new ContentParsingOptions
            {
                MaxRecords = maxRecords,
                MaxCsvColumns = maxCsvColumns,
                MaxFieldLength = maxFieldLength
            });

        return new CsvContentParser(options);
    }
}