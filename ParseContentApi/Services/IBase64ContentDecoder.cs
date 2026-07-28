namespace ParseContentApi.Services;

public interface IBase64ContentDecoder
{
    string Decode(string encodedContent);
}