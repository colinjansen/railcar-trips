using RailcarTrips.Domain.Models;
using RailcarTrips.Domain.Services;

namespace RailcarTrips.UnitTests;

public sealed class TripProcessingRulesTests
{
    [Fact]
    public void SelectEventsToPersist_SkipsDuplicatesWithinSameCandidateSet()
    {
        var timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var first = new EquipmentEvent
        {
            EquipmentId = "CAR1",
            EventCode = "W",
            EventUtcTime = timestamp,
            EventLocalTime = timestamp,
            CityId = 1
        };
        var duplicate = new EquipmentEvent
        {
            EquipmentId = "CAR1",
            EventCode = "W",
            EventUtcTime = timestamp,
            EventLocalTime = timestamp,
            CityId = 1
        };

        var result = TripProcessingRules.SelectEventsToPersist(
            [first, duplicate],
            new HashSet<EventKey>());

        Assert.Single(result.Events);
        Assert.Equal("DuplicateEvent", Assert.Single(result.Warnings).Code);
    }

    [Fact]
    public void BuildEvents_ReportsUnknownCityAsError()
    {
        var rows = new[]
        {
            new ImportedEventRow("CAR1", "W", new DateTime(2026, 1, 1, 0, 0, 0), 99, "row", 2)
        };
        var cities = new Dictionary<int, City>();

        var result = TripProcessingRules.BuildEvents(rows, cities, new StubEventTimeConverter());

        Assert.Empty(result.Events);
        var issue = Assert.Single(result.Issues);
        Assert.Equal("UnknownCity", issue.Code);
        Assert.Equal(ProcessingIssueSeverity.Error, issue.Severity);
    }

    [Fact]
    public void BuildEvents_ReportsMissingTimeZoneAsError()
    {
        var rows = new[]
        {
            new ImportedEventRow("CAR1", "W", new DateTime(2026, 1, 1, 0, 0, 0), 1, "row", 2)
        };
        var cities = new Dictionary<int, City>
        {
            [1] = new() { Id = 1, Name = "Alpha", TimeZoneId = "Missing" }
        };

        var converter = new StubEventTimeConverter();
        var result = TripProcessingRules.BuildEvents(rows, cities, converter);

        Assert.Empty(result.Events);
        var issue = Assert.Single(result.Issues);
        Assert.Equal("TimeZoneNotFound", issue.Code);
        Assert.Equal(ProcessingIssueSeverity.Error, issue.Severity);
    }

    [Fact]
    public void BuildEvents_ReportsAdjustedInvalidLocalTimeAsWarning()
    {
        var local = new DateTime(2026, 3, 8, 2, 30, 0);
        var adjusted = new DateTime(2026, 3, 8, 3, 30, 0);
        var utc = new DateTime(2026, 3, 8, 10, 30, 0, DateTimeKind.Utc);
        var rows = new[]
        {
            new ImportedEventRow("CAR1", "W", local, 1, "row", 2)
        };
        var cities = new Dictionary<int, City>
        {
            [1] = new() { Id = 1, Name = "Alpha", TimeZoneId = "Test/Invalid" }
        };

        var converter = new StubEventTimeConverter();
        converter.Add("Test/Invalid", new EventTimeConversionResult(adjusted, utc, true));

        var result = TripProcessingRules.BuildEvents(rows, cities, converter);

        var issue = Assert.Single(result.Issues);
        Assert.Equal("InvalidLocalTimeAdjusted", issue.Code);
        Assert.Equal(ProcessingIssueSeverity.Warning, issue.Severity);
        Assert.Equal(adjusted, Assert.Single(result.Events).EventLocalTime);
    }

    [Fact]
    public void SelectTripsToPersist_RemovesDuplicateTrips_AndRelatedTripEvents()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var events = new List<EquipmentEvent>
        {
            new() { EquipmentId = "CAR1", EventCode = "W", CityId = 1, EventUtcTime = start },
            new() { EquipmentId = "CAR1", EventCode = "Z", CityId = 2, EventUtcTime = end }
        };
        var existing = new HashSet<TripKey>
        {
            new("CAR1", start, end)
        };

        var result = TripProcessingRules.SelectTripsToPersist(events, existing);

        Assert.Empty(result.Trips);
        Assert.Empty(result.TripEvents);
        Assert.Contains(result.Warnings, warning => warning.Code == "DuplicateTrip");
    }

    private sealed class StubEventTimeConverter : IEventTimeConverter
    {
        private readonly Dictionary<string, EventTimeConversionResult> _map = new(StringComparer.OrdinalIgnoreCase);

        public void Add(string timeZoneId, EventTimeConversionResult conversionResult) => _map[timeZoneId] = conversionResult;

        public EventTimeConversionResult? Convert(DateTime eventLocalTime, string timeZoneId) =>
            _map.TryGetValue(timeZoneId, out var result) ? result : null;
    }
}
