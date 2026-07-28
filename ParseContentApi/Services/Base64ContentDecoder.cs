using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ParseContentApi.Exceptions;
using ParseContentApi.Options;

namespace ParseContentApi.Services;

public sealed class Base64ContentDecoder : IBase64ContentDecoder
{
    private static readonly UTF8Encoding StrictUtf8Encoding = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private readonly ContentParsingOptions _options;

    public Base64ContentDecoder(IOptions<ContentParsingOptions> options)
    {
        _options = options.Value;
    }

    public string Decode(string encodedContent)
    {
        ArgumentNullException.ThrowIfNull(encodedContent);

        if (encodedContent.Length > _options.MaxBase64Length)
        {
            throw new PayloadTooLargeException(
                $"The Base64 content exceeds the maximum length of " +
                $"{_options.MaxBase64Length} characters.");
        }

        var estimatedDecodedLength =
            ((encodedContent.Length + 3L) / 4L) * 3L;

        if (estimatedDecodedLength > _options.MaxDecodedBytes)
        {
            throw new PayloadTooLargeException(
                $"The decoded content may exceed the maximum size of " +
                $"{_options.MaxDecodedBytes} bytes.");
        }

        var bufferLength = Math.Max(1, checked((int)estimatedDecodedLength));
        var rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferLength);
        var bytesWritten = 0;

        try
        {
            if (!Convert.TryFromBase64String(
                    encodedContent,
                    rentedBuffer,
                    out bytesWritten))
            {
                throw new ContentParsingException(
                    "The content field does not contain valid Base64 data.");
            }

            if (bytesWritten > _options.MaxDecodedBytes)
            {
                throw new PayloadTooLargeException(
                    $"The decoded content exceeds the maximum size of " +
                    $"{_options.MaxDecodedBytes} bytes.");
            }

            string decodedContent;

            try
            {
                decodedContent = StrictUtf8Encoding.GetString(
                    rentedBuffer,
                    0,
                    bytesWritten);
            }
            catch (DecoderFallbackException exception)
            {
                throw new ContentParsingException(
                    "The decoded content is not valid UTF-8 text.",
                    exception);
            }

            // Remove the optional UTF-8 byte order mark.
            return decodedContent.Length > 0 &&
                   decodedContent[0] == '\uFEFF'
                ? decodedContent[1..]
                : decodedContent;
        }
        finally
        {
            if (bytesWritten > 0)
            {
                CryptographicOperations.ZeroMemory(
                    rentedBuffer.AsSpan(0, bytesWritten));
            }

            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }
}