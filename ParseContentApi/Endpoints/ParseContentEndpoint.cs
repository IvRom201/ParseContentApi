using System.Net.Http.Headers;
using ParseContentApi.Contracts;
using ParseContentApi.Services;

namespace ParseContentApi.Endpoints;

public static class ParseContentEndpoint
{
    public static IEndpointRouteBuilder MapParseContentEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost(
                "/api/v1/parse-content",
                HandleAsync)
            .WithName("ParseContent")
            .Accepts<ParseContentRequest>("application/json")
            .Produces<ParseContentResponse>(
                StatusCodes.Status200OK,
                "application/json")
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(
                StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(
                StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        HttpRequest httpRequest,
        IBase64ContentDecoder decoder,
        IContentParserResolver parserResolver,
        ParseContentRequestValidator validator,
        CancellationToken cancellationToken)
    {
        if (!IsApplicationJson(httpRequest.ContentType))
        {
            return TypedResults.Problem(
                title: "Unsupported media type",
                detail:
                    "The Content-Type header must be application/json.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        var request = await httpRequest
            .ReadFromJsonAsync<ParseContentRequest>(
                cancellationToken: cancellationToken);

        if (request is null)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["body"] =
                    [
                        "The request body is required."
                    ]
                });
        }

        var validationErrors = validator.Validate(request);

        if (validationErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var format = request.Type!.Value;
        var decodedContent = decoder.Decode(request.Content!);

        var parser = parserResolver.Resolve(format);
        var parsedContent = parser.Parse(decodedContent);

        var response = new ParseContentResponse(
            Status: "success",
            Type: format,
            ProcessedCount: parsedContent.Records.Count,
            Data: parsedContent.Records);

        return TypedResults.Ok(response);
    }

    private static bool IsApplicationJson(string? contentType)
    {
        if (!MediaTypeHeaderValue.TryParse(
                contentType,
                out var parsedContentType))
        {
            return false;
        }

        return string.Equals(
            parsedContentType.MediaType,
            "application/json",
            StringComparison.OrdinalIgnoreCase);
    }
}