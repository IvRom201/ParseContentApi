using ParseContentApi.Contracts;
using ParseContentApi.Exceptions;
using ParseContentApi.Options;
using ParseContentApi.Services;

namespace ParseContentApi.Tests.Unit;

public sealed class ContentParserResolverTests
{
    [Fact]
    public void Resolve_CsvFormat_ReturnsCsvParser()
    {
        var resolver = CreateResolver();

        var parser = resolver.Resolve(ContentFormat.Csv);

        Assert.IsType<CsvContentParser>(parser);
    }

    [Fact]
    public void Resolve_InternalJsonFormat_ReturnsInternalJsonParser()
    {
        var resolver = CreateResolver();

        var parser = resolver.Resolve(ContentFormat.InternalJson);

        Assert.IsType<InternalJsonContentParser>(parser);
    }

    [Fact]
    public void Resolve_UnknownFormat_ThrowsUnsupportedContentFormatException()
    {
        var resolver = CreateResolver();

        Assert.Throws<UnsupportedContentFormatException>(
            () => resolver.Resolve(ContentFormat.Unknown));
    }

    private static ContentParserResolver CreateResolver()
    {
        var options = OptionsFactory.Create(
            new ContentParsingOptions());

        IContentParser[] parsers =
        [
            new CsvContentParser(options),
            new InternalJsonContentParser(options)
        ];

        return new ContentParserResolver(parsers);
    }
}