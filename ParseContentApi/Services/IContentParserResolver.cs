using ParseContentApi.Contracts;

namespace ParseContentApi.Services;

public interface IContentParserResolver
{
    IContentParser Resolve(ContentFormat format);
}