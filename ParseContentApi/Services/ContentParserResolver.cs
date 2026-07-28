using ParseContentApi.Contracts;
using ParseContentApi.Exceptions;

namespace ParseContentApi.Services;

public sealed class ContentParserResolver : IContentParserResolver
{
    private readonly IReadOnlyDictionary<ContentFormat, IContentParser> _parsers;

    public ContentParserResolver(IEnumerable<IContentParser> parsers)
    {
        ArgumentNullException.ThrowIfNull(parsers);

        _parsers = parsers.ToDictionary(
            parser => parser.SupportedFormat,
            parser => parser);
    }

    public IContentParser Resolve(ContentFormat format)
    {
        if (_parsers.TryGetValue(format, out var parser))
        {
            return parser;
        }

        throw new UnsupportedContentFormatException(format);
    }
}