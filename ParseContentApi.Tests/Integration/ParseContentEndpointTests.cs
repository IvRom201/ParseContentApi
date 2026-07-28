using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ParseContentApi.Tests.Integration;

public sealed class ParseContentEndpointTests :
    IClassFixture<WebApplicationFactory<Program>>,
    IDisposable
{
    private const string EndpointPath = "/api/v1/parse-content";

    private readonly HttpClient _client;

    public ParseContentEndpointTests(
        WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
    }

    [Fact]
    public async Task Post_ValidCsv_ReturnsSuccessfulResponse()
    {
        const string csv =
            """
            name,age
            Alice,30
            Bob,28
            """;

        var request = new
        {
            type = "CSV",
            content = ToBase64(csv)
        };

        using var response = await _client.PostAsJsonAsync(
            EndpointPath,
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await ReadJsonAsync(response);
        var root = document.RootElement;

        Assert.Equal(
            "success",
            root.GetProperty("status").GetString());

        Assert.Equal(
            "CSV",
            root.GetProperty("type").GetString());

        Assert.Equal(
            2,
            root.GetProperty("processedCount").GetInt32());

        var records = root
            .GetProperty("data")
            .EnumerateArray()
            .ToArray();

        Assert.Equal(2, records.Length);

        Assert.Equal(
            "Alice",
            records[0].GetProperty("name").GetString());

        Assert.Equal(
            "30",
            records[0].GetProperty("age").GetString());
    }

    [Fact]
    public async Task Post_ValidInternalJson_ReturnsSuccessfulResponse()
    {
        const string internalJson =
            """
            [
              {
                "id": 1,
                "name": "Alice"
              },
              {
                "id": 2,
                "name": "Bob"
              }
            ]
            """;

        var request = new
        {
            type = "INTERNAL_JSON",
            content = ToBase64(internalJson)
        };

        using var response = await _client.PostAsJsonAsync(
            EndpointPath,
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await ReadJsonAsync(response);
        var root = document.RootElement;

        Assert.Equal(
            "INTERNAL_JSON",
            root.GetProperty("type").GetString());

        Assert.Equal(
            2,
            root.GetProperty("processedCount").GetInt32());

        var records = root
            .GetProperty("data")
            .EnumerateArray()
            .ToArray();

        Assert.Equal(
            1,
            records[0].GetProperty("id").GetInt32());

        Assert.Equal(
            "Bob",
            records[1].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Post_UnsupportedContentType_Returns415()
    {
        const string json =
            """
            {
              "type": "CSV",
              "content": "bmFtZQpBbGljZQ=="
            }
            """;

        using var content = new StringContent(
            json,
            Encoding.UTF8,
            "text/plain");

        using var response = await _client.PostAsync(
            EndpointPath,
            content);

        Assert.Equal(
            HttpStatusCode.UnsupportedMediaType,
            response.StatusCode);
    }

    [Fact]
    public async Task Post_UnsupportedFormat_Returns400()
    {
        var request = new
        {
            type = "XML",
            content = ToBase64("<item />")
        };

        using var response = await _client.PostAsJsonAsync(
            EndpointPath,
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using var document = await ReadJsonAsync(response);

        var errors = document.RootElement.GetProperty("errors");

        Assert.True(errors.TryGetProperty("type", out _));
    }

    [Fact]
    public async Task Post_MissingContent_Returns400()
    {
        var request = new
        {
            type = "CSV"
        };

        using var response = await _client.PostAsJsonAsync(
            EndpointPath,
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using var document = await ReadJsonAsync(response);

        var errors = document.RootElement.GetProperty("errors");

        Assert.True(errors.TryGetProperty("content", out _));
    }

    [Fact]
    public async Task Post_InvalidBase64_Returns400ProblemDetails()
    {
        var request = new
        {
            type = "CSV",
            content = "This is not Base64!"
        };

        using var response = await _client.PostAsJsonAsync(
            EndpointPath,
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = await ReadJsonAsync(response);
        var root = document.RootElement;

        Assert.Equal(
            "Content parsing failed",
            root.GetProperty("title").GetString());

        Assert.True(root.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Post_InvalidInternalJson_Returns400ProblemDetails()
    {
        const string invalidInternalJson =
            """
            {
              "id": 1,
            }
            """;

        var request = new
        {
            type = "INTERNAL_JSON",
            content = ToBase64(invalidInternalJson)
        };

        using var response = await _client.PostAsJsonAsync(
            EndpointPath,
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using var document = await ReadJsonAsync(response);

        Assert.Equal(
            "Content parsing failed",
            document.RootElement
                .GetProperty("title")
                .GetString());
    }

    [Fact]
    public async Task Post_MalformedRequestJson_Returns400()
    {
        const string malformedRequest =
            """
            {
              "type": "CSV",
              "content":
            }
            """;

        using var content = new StringContent(
            malformedRequest,
            Encoding.UTF8,
            "application/json");

        using var response = await _client.PostAsync(
            EndpointPath,
            content);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private static string ToBase64(string value)
    {
        return Convert.ToBase64String(
            Encoding.UTF8.GetBytes(value));
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();

        return JsonDocument.Parse(json);
    }
}