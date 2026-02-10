namespace RailcarTrips.Application.Abstractions;

public interface ICsvReader
{
    Task<List<string[]>> ReadRows(Stream stream, bool skipHeaderRow = false, bool leaveStreamOpen = false, CancellationToken cancellationToken = default);
}