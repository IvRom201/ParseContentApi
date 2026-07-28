using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ParseContentApi.Exceptions;

namespace ParseContentApi.Infrastructure;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var problemDetails = CreateProblemDetails(
            httpContext,
            exception);

        if (problemDetails.Status >= 500)
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "The request could not be processed. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode =
            problemDetails.Status ??
            StatusCodes.Status500InternalServerError;

        httpContext.Response.StatusCode =
            problemDetails.Status ??
            StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception)
    {
        var problemDetails = exception switch
        {
            PayloadTooLargeException => new ProblemDetails
            {
                Status = StatusCodes.Status413PayloadTooLarge,
                Title = "Payload too large",
                Detail = exception.Message
            },

            UnsupportedContentFormatException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unsupported content format",
                Detail = exception.Message
            },

            ContentParsingException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Content parsing failed",
                Detail = exception.Message
            },

            JsonException jsonException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid JSON request",
                Detail = CreateJsonErrorDetail(jsonException)
            },

            BadHttpRequestException badRequestException =>
                new ProblemDetails
                {
                    Status = badRequestException.StatusCode,
                    Title = "Invalid HTTP request",
                    Detail = "The HTTP request could not be processed."
                },

            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal server error",
                Detail =
                    "An unexpected error occurred while processing " +
                    "the request."
            }
        };

        problemDetails.Instance = httpContext.Request.Path;

        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id ??
            httpContext.TraceIdentifier;

        return problemDetails;
    }

    private static string CreateJsonErrorDetail(
        JsonException exception)
    {
        return string.IsNullOrWhiteSpace(exception.Path)
            ? "The request body does not contain valid JSON."
            : $"The request body contains invalid JSON at " +
              $"path '{exception.Path}'.";
    }
}