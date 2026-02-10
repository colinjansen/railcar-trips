using RailcarTrips.Domain.Models;

namespace RailcarTrips.UnitTests;

public sealed class TripAggregateTests
{
    [Fact]
    public void BuildTrips_CreatesTrip_ForStartAndEnd()
    {
        var events = new List<EquipmentEvent>
        {
            new() { EquipmentId = "CAR1", EventCode = "W", CityId = 1, EventUtcTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new() { EquipmentId = "CAR1", EventCode = "Z", CityId = 2, EventUtcTime = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) }
        };

        var result = Trip.BuildTrips(events);

        Assert.Single(result.Trips);
        var trip = result.Trips[0];
        Assert.Equal("CAR1", trip.EquipmentId);
        Assert.Equal(1, trip.OriginCityId);
        Assert.Equal(2, trip.DestinationCityId);
        Assert.Equal(24, trip.TotalTripHours, 1);
        Assert.Equal(2, trip.TripEvents.Count);
    }

    [Fact]
    public void BuildTrips_SortsEventsByUtc()
    {
        var events = new List<EquipmentEvent>
        {
            new() { EquipmentId = "CAR1", EventCode = "Z", CityId = 2, EventUtcTime = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
            new() { EquipmentId = "CAR1", EventCode = "W", CityId = 1, EventUtcTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        };

        var result = Trip.BuildTrips(events);

        Assert.Single(result.Trips);
        Assert.DoesNotContain(result.Warnings, w => w.Code == "EndWithoutStart");
    }

    [Fact]
    public void BuildTrips_ReportsMissingEnd()
    {
        var events = new List<EquipmentEvent>
        {
            new() { EquipmentId = "CAR1", EventCode = "W", CityId = 1, EventUtcTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        };

        var result = Trip.BuildTrips(events);

        Assert.Empty(result.Trips);
        Assert.Contains(result.Warnings, w => w.Code == "StartWithoutEnd");
    }

    [Fact]
    public void BuildTrips_ReportsEndWithoutStart()
    {
        var events = new List<EquipmentEvent>
        {
            new() { EquipmentId = "CAR1", EventCode = "Z", CityId = 2, EventUtcTime = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) }
        };

        var result = Trip.BuildTrips(events);

        Assert.Empty(result.Trips);
        Assert.Contains(result.Warnings, w => w.Code == "EndWithoutStart");
    }

    [Fact]
    public void BuildTrips_ReportsOverlappingStart()
    {
        var events = new List<EquipmentEvent>
        {
            new() { EquipmentId = "CAR1", EventCode = "W", CityId = 1, EventUtcTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new() { EquipmentId = "CAR1", EventCode = "W", CityId = 1, EventUtcTime = new DateTime(2026, 1, 1, 1, 0, 0, DateTimeKind.Utc) }
        };

        var result = Trip.BuildTrips(events);

        Assert.Empty(result.Trips);
        Assert.Contains(result.Warnings, w => w.Code == "OverlappingStart");
    }
}
