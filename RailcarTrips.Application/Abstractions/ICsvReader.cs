using RailcarTrips.Domain.Models;

namespace RailcarTrips.Application.Abstractions;

public interface ICsvReader
{
    Task<CsvReadResult> ReadRows(
        Stream stream,
        bool skipHeaderRow = false,
        bool leaveStreamOpen = false,
        CancellationToken cancellationToken = default);
}

public sealed record CsvReadResult(
    IReadOnlyList<ImportedEventRow> Rows,
    IReadOnlyList<ProcessingIssue> Issues);
