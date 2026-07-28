using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using ParseContentApi.Contracts;
using ParseContentApi.Exceptions;
using ParseContentApi.Options;

namespace ParseContentApi.Services;

public sealed class CsvContentParser : IContentParser
{
    private readonly ContentParsingOptions _options;

    public CsvContentParser(IOptions<ContentParsingOptions> options)
    {
        _options = options.Value;
    }

    public ContentFormat SupportedFormat => ContentFormat.Csv;

    public ParsedContent Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var rows = ParseRows(content);

        if (rows.Count == 0)
        {
            throw new ContentParsingException(
                "The CSV content does not contain a header row.");
        }

        var headers = rows[0]
            .Select(header => header.Trim())
            .ToArray();

        ValidateHeaders(headers);

        var records = new List<JsonObject>(
            capacity: Math.Max(0, rows.Count - 1));

        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];

            if (row.Count != headers.Length)
            {
                throw new ContentParsingException(
                    $"CSV row {rowIndex + 1} contains {row.Count} fields, " +
                    $"but the header contains {headers.Length} fields.");
            }

            var record = new JsonObject();

            for (var columnIndex = 0;
                 columnIndex < headers.Length;
                 columnIndex++)
            {
                record[headers[columnIndex]] =
                    JsonValue.Create(row[columnIndex]);
            }

            records.Add(record);
        }

        return new ParsedContent(records);
    }

    private List<List<string>> ParseRows(string content)
    {
        var rows = new List<List<string>>();
        var currentRow = new List<string>();
        var currentField = new StringBuilder();

        var insideQuotedField = false;
        var atFieldStart = true;
        var endedWithRowSeparator = false;
        var rowHasMeaningfulSyntax = false;

        for (var index = 0; index < content.Length; index++)
        {
            var character = content[index];

            if (insideQuotedField)
            {
                endedWithRowSeparator = false;
                rowHasMeaningfulSyntax = true;

                if (character == '"')
                {
                    var isEscapedQuote =
                        index + 1 < content.Length &&
                        content[index + 1] == '"';

                    if (isEscapedQuote)
                    {
                        AppendCharacter(currentField, '"');
                        index++;
                    }
                    else
                    {
                        insideQuotedField = false;
                    }

                    continue;
                }

                AppendCharacter(currentField, character);
                continue;
            }

            switch (character)
            {
                case '"' when atFieldStart:
                    insideQuotedField = true;
                    atFieldStart = false;
                    endedWithRowSeparator = false;
                    rowHasMeaningfulSyntax = true;
                    break;

                case '"':
                    throw new ContentParsingException(
                        "The CSV content contains an unexpected quote.");

                case ',':
                    CompleteField(currentRow, currentField);

                    atFieldStart = true;
                    endedWithRowSeparator = false;
                    rowHasMeaningfulSyntax = true;
                    break;

                case '\r':
                case '\n':
                    CompleteField(currentRow, currentField);
                    CompleteRow(
                        rows,
                        currentRow,
                        rowHasMeaningfulSyntax);

                    atFieldStart = true;
                    rowHasMeaningfulSyntax = false;
                    endedWithRowSeparator = true;

                    // Treat CRLF as one row separator.
                    if (character == '\r' &&
                        index + 1 < content.Length &&
                        content[index + 1] == '\n')
                    {
                        index++;
                    }

                    break;

                default:
                    AppendCharacter(currentField, character);

                    atFieldStart = false;
                    endedWithRowSeparator = false;

                    if (!char.IsWhiteSpace(character))
                    {
                        rowHasMeaningfulSyntax = true;
                    }

                    break;
            }
        }

        if (insideQuotedField)
        {
            throw new ContentParsingException(
                "The CSV content contains an unclosed quoted field.");
        }

        if (!endedWithRowSeparator && content.Length > 0)
        {
            CompleteField(currentRow, currentField);
            CompleteRow(
                rows,
                currentRow,
                rowHasMeaningfulSyntax);
        }

        return rows;
    }

    private void AppendCharacter(
        StringBuilder currentField,
        char character)
    {
        currentField.Append(character);

        if (currentField.Length > _options.MaxFieldLength)
        {
            throw new PayloadTooLargeException(
                $"A CSV field exceeds the maximum length of " +
                $"{_options.MaxFieldLength} characters.");
        }
    }

    private void CompleteField(
        ICollection<string> currentRow,
        StringBuilder currentField)
    {
        currentRow.Add(currentField.ToString());
        currentField.Clear();

        if (currentRow.Count > _options.MaxCsvColumns)
        {
            throw new PayloadTooLargeException(
                $"A CSV row exceeds the maximum number of " +
                $"{_options.MaxCsvColumns} columns.");
        }
    }

    private void CompleteRow(
        ICollection<List<string>> rows,
        List<string> currentRow,
        bool rowHasMeaningfulSyntax)
    {
        var isBlankLine =
            !rowHasMeaningfulSyntax &&
            currentRow.Count == 1 &&
            string.IsNullOrWhiteSpace(currentRow[0]);

        if (!isBlankLine)
        {
            rows.Add(new List<string>(currentRow));

            // The first row is the header and is not counted as a data record.
            if (rows.Count > _options.MaxRecords + 1)
            {
                throw new PayloadTooLargeException(
                    $"The CSV content exceeds the maximum number of " +
                    $"{_options.MaxRecords} data rows.");
            }
        }

        currentRow.Clear();
    }

    private static void ValidateHeaders(IReadOnlyList<string> headers)
    {
        if (headers.Count == 0)
        {
            throw new ContentParsingException(
                "The CSV header row is empty.");
        }

        for (var index = 0; index < headers.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(headers[index]))
            {
                throw new ContentParsingException(
                    $"CSV header number {index + 1} is empty.");
            }
        }

        var duplicateHeader = headers
            .GroupBy(
                header => header,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateHeader is not null)
        {
            throw new ContentParsingException(
                $"The CSV header '{duplicateHeader.Key}' is duplicated.");
        }
    }
}