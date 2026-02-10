using System.Text;
using RailcarTrips.Domain.Models;
using RailcarTrips.Infrastructure.Services;

namespace RailcarTrips.UnitTests;

public sealed class CsvReaderTests
{
    [Fact]
    public async Task ReadRows_ParsesValidRows_AndReportsInvalidRowsAsWarnings()
    {
        var reader = new CsvReader();
        var csv = string.Join('\n', new[]
        {
            "Equipment Id,Event Code,Event Time,City Id",
            "CAR1,W,2026-01-01 00:00,1",
            "bad,row",
            "CAR2,Z,not-a-date,1",
            "CAR3,Z,2026-01-01 00:00,not-an-int"
        });

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await reader.ReadRows(stream, skipHeaderRow: true, leaveStreamOpen: true);

        Assert.Single(result.Rows);
        Assert.Equal("CAR1", result.Rows[0].EquipmentId);
        Assert.Equal(3, result.Issues.Count);
        Assert.All(result.Issues, issue => Assert.Equal(ProcessingIssueSeverity.Warning, issue.Severity));
    }
}
