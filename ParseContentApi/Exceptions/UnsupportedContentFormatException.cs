using ParseContentApi.Contracts;

namespace ParseContentApi.Exceptions;

public sealed class UnsupportedContentFormatException : Exception
{
    public UnsupportedContentFormatException(ContentFormat format) 
        : base($"The content format '{format}' is not supported.") {}
}