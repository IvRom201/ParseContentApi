using ParseContentApi.Contracts;

namespace ParseContentApi.Services;

public interface IContentParser
{
    ContentFormat SupportedFormat { get; }

    ParsedContent Parse(string content);
}