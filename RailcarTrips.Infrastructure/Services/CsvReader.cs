using RailcarTrips.Application.Abstractions;

public sealed class CsvReader : ICsvReader
{
    public async Task<List<string[]>> ReadRows(Stream stream, bool skipHeaderRow = false, bool leaveStreamOpen = false, CancellationToken cancellationToken = default)
    {
        var rows = new List<string[]>();
        using var reader = new StreamReader(stream, leaveOpen: leaveStreamOpen);

        if (skipHeaderRow)
        {
            await reader.ReadLineAsync(cancellationToken);
        }

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            var parts = line.Split(',');
            rows.Add(parts);
        }

        return rows;
    }
}