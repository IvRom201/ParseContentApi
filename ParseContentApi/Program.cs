using System.Text.Json;
using System.Text.Json.Serialization;
using ParseContentApi.Endpoints;
using ParseContentApi.Infrastructure;
using ParseContentApi.Options;
using ParseContentApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 16 * 1024 * 1024;
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition =
        JsonIgnoreCondition.WhenWritingNull;
});

builder.Services
    .AddOptions<ContentParsingOptions>()
    .BindConfiguration(ContentParsingOptions.SectionName)
    .Validate(
        options => options.MaxBase64Length > 0,
        "MaxBase64Length must be greater than zero.")
    .Validate(
        options => options.MaxDecodedBytes > 0,
        "MaxDecodedBytes must be greater than zero.")
    .Validate(
        options => options.MaxRecords > 0,
        "MaxRecords must be greater than zero.")
    .Validate(
        options => options.MaxCsvColumns > 0,
        "MaxCsvColumns must be greater than zero.")
    .Validate(
        options => options.MaxFieldLength > 0,
        "MaxFieldLength must be greater than zero.")
    .Validate(
        options => options.MaxJsonDepth is > 0 and <= 256,
        "MaxJsonDepth must be between 1 and 256.")
    .ValidateOnStart();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSingleton<IBase64ContentDecoder, Base64ContentDecoder>();

builder.Services.AddSingleton<IContentParser, CsvContentParser>();
builder.Services.AddSingleton<IContentParser, InternalJsonContentParser>();
builder.Services.AddSingleton<IContentParserResolver, ContentParserResolver>();

builder.Services.AddSingleton<ParseContentRequestValidator>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.MapParseContentEndpoint();

app.Run();
public partial class Program;