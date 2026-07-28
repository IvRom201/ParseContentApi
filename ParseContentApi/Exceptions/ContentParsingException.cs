namespace ParseContentApi.Exceptions;

public sealed class ContentParsingException : Exception
{
    public ContentParsingException(string message)
        : base(message)
    {
    }

    public ContentParsingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}