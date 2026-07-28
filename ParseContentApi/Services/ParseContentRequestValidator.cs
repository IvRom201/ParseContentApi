using ParseContentApi.Contracts;

namespace ParseContentApi.Services;

public sealed class ParseContentRequestValidator
{
    public Dictionary<string, string[]> Validate(
        ParseContentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, string[]>(
            StringComparer.OrdinalIgnoreCase);

        if (request.Type is null)
        {
            errors["type"] =
            [
                "The type field is required."
            ];
        }
        else if (request.Type == ContentFormat.Unknown)
        {
            errors["type"] =
            [
                "The supplied type is not supported. " +
                "Supported values are CSV and INTERNAL_JSON."
            ];
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            errors["content"] =
            [
                "The content field is required and must contain Base64 data."
            ];
        }

        return errors;
    }
}