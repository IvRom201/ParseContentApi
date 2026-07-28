namespace ParseContentApi.Options;

public sealed class ContentParsingOptions
{
    public const string SectionName = "ContentParsing";

    public int MaxBase64Length { get; init; } = 14_000_000;

    public int MaxDecodedBytes { get; init; } = 10_000_000;

    public int MaxRecords { get; init; } = 10_000;

    public int MaxCsvColumns { get; init; } = 500;

    public int MaxFieldLength { get; init; } = 1_000_000;

    public int MaxJsonDepth { get; init; } = 64;
}