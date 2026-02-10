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
}
