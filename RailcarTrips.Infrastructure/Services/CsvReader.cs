using System.Globalization;
using RailcarTrips.Application.Abstractions;
using RailcarTrips.Domain.Models;

namespace RailcarTrips.Infrastructure.Services;

public sealed class CsvReader : ICsvReader
{
    public async Task<CsvReadResult> ReadRows(
        Stream stream,
        bool skipHeaderRow = false,
        bool leaveStreamOpen = false,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<ImportedEventRow>();
        var issues = new List<ProcessingIssue>();
        using var reader = new StreamReader(stream, leaveOpen: leaveStreamOpen);

        var lineNumber = 0;
        if (skipHeaderRow)
        {
            await reader.ReadLineAsync(cancellationToken);
            lineNumber++;
        }

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 4)
            {
                issues.Add(new ProcessingIssue(
                    "InvalidCsvRow",
                    $"Skipping invalid row (line {lineNumber}): {line}",
                    ProcessingIssueSeverity.Warning));
                continue;
            }

            if (!DateTime.TryParseExact(parts[2], "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var localTime))
            {
                issues.Add(new ProcessingIssue(
                    "InvalidEventDate",
                    $"Skipping row with invalid date (line {lineNumber}): {line}",
                    ProcessingIssueSeverity.Warning));
                continue;
            }

            if (!int.TryParse(parts[3], out var cityId))
            {
                issues.Add(new ProcessingIssue(
                    "InvalidCityId",
                    $"Skipping row with invalid city id (line {lineNumber}): {line}",
                    ProcessingIssueSeverity.Warning));
                continue;
            }

            rows.Add(new ImportedEventRow(
                parts[0],
                parts[1],
                localTime,
                cityId,
                line,
                lineNumber));
        }

        return new CsvReadResult(rows, issues);
    }
}
