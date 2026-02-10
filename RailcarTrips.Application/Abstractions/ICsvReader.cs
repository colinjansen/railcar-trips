using RailcarTrips.Domain.Models;

namespace RailcarTrips.Application.Abstractions;

public interface ICsvReader
{
    /// <summary>
    /// Reads rows from a CSV stream and returns the result, including any processing issues encountered.
    /// </summary>
    /// <param name="stream">The stream containing CSV data.</param>
    /// <param name="skipHeaderRow">Indicates whether to skip the first row, typically used for headers.</param>
    /// <param name="leaveStreamOpen">Indicates whether to leave the stream open after reading.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the imported rows and any processing issues.</returns>
    Task<CsvReadResult> ReadRows(
        Stream stream,
        bool skipHeaderRow = false,
        bool leaveStreamOpen = false,
        CancellationToken cancellationToken = default);
}

public sealed record CsvReadResult(
    IReadOnlyList<ImportedEventRow> Rows,
    IReadOnlyList<ProcessingIssue> Issues);
