using System.Text;
using ParseContentApi.Exceptions;
using ParseContentApi.Options;
using ParseContentApi.Services;

namespace ParseContentApi.Tests.Unit;

public sealed class Base64ContentDecoderTests
{
    [Fact]
    public void Decode_ValidBase64_ReturnsDecodedUtf8Text()
    {
        var decoder = CreateDecoder();
        var encodedContent = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("Hello, world!"));

        var result = decoder.Decode(encodedContent);

        Assert.Equal("Hello, world!", result);
    }

    [Fact]
    public void Decode_UnicodeText_ReturnsDecodedText()
    {
        var decoder = CreateDecoder();
        var encodedContent = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("Привет, мир!"));

        var result = decoder.Decode(encodedContent);

        Assert.Equal("Привет, мир!", result);
    }

    [Fact]
    public void Decode_ContentWithUtf8Bom_RemovesBom()
    {
        var decoder = CreateDecoder();

        var preamble = Encoding.UTF8.GetPreamble();
        var textBytes = Encoding.UTF8.GetBytes("Content");

        var bytes = new byte[preamble.Length + textBytes.Length];

        preamble.CopyTo(bytes, 0);
        textBytes.CopyTo(bytes, preamble.Length);

        var encodedContent = Convert.ToBase64String(bytes);

        var result = decoder.Decode(encodedContent);

        Assert.Equal("Content", result);
    }

    [Fact]
    public void Decode_InvalidBase64_ThrowsContentParsingException()
    {
        var decoder = CreateDecoder();

        var exception = Assert.Throws<ContentParsingException>(
            () => decoder.Decode("This is not Base64!"));

        Assert.Equal(
            "The content field does not contain valid Base64 data.",
            exception.Message);
    }

    [Fact]
    public void Decode_InvalidUtf8_ThrowsContentParsingException()
    {
        var decoder = CreateDecoder();

        var invalidUtf8Bytes = new byte[]
        {
            0xFF,
            0xFE,
            0xFA
        };

        var encodedContent = Convert.ToBase64String(invalidUtf8Bytes);

        var exception = Assert.Throws<ContentParsingException>(
            () => decoder.Decode(encodedContent));

        Assert.Equal(
            "The decoded content is not valid UTF-8 text.",
            exception.Message);
    }

    [Fact]
    public void Decode_Base64ExceedsConfiguredLimit_ThrowsPayloadTooLargeException()
    {
        var decoder = CreateDecoder(maxBase64Length: 4);

        var encodedContent = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("Hello"));

        Assert.Throws<PayloadTooLargeException>(
            () => decoder.Decode(encodedContent));
    }

    [Fact]
    public void Decode_DecodedContentExceedsConfiguredLimit_ThrowsPayloadTooLargeException()
    {
        var decoder = CreateDecoder(maxDecodedBytes: 4);

        var encodedContent = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("Hello"));

        Assert.Throws<PayloadTooLargeException>(
            () => decoder.Decode(encodedContent));
    }

    [Fact]
    public void Decode_NullContent_ThrowsArgumentNullException()
    {
        var decoder = CreateDecoder();

        Assert.Throws<ArgumentNullException>(
            () => decoder.Decode(null!));
    }

    private static Base64ContentDecoder CreateDecoder(
        int maxBase64Length = 10_000,
        int maxDecodedBytes = 10_000)
    {
        var options = OptionsFactory.Create(
            new ContentParsingOptions
            {
                MaxBase64Length = maxBase64Length,
                MaxDecodedBytes = maxDecodedBytes
            });

        return new Base64ContentDecoder(options);
    }
}