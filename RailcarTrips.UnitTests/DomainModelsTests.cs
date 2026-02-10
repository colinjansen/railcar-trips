using RailcarTrips.Domain.Models;

namespace RailcarTrips.UnitTests;

public sealed class DomainModelsTests
{
    [Fact]
    public void Models_CanSetAndReadProperties()
    {
        var city = new City { Id = 1, Name = "Alpha", TimeZoneId = "UTC" };
        var equipmentEvent = new EquipmentEvent
        {
            Id = 2,
            EquipmentId = "CAR1",
            EventCode = "W",
            EventLocalTime = new DateTime(2026, 1, 1, 0, 0, 0),
            EventUtcTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CityId = city.Id,
            City = city
        };
        var trip = new Trip
        {
            Id = 3,
            EquipmentId = "CAR1",
            OriginCityId = city.Id,
            DestinationCityId = 2,
            StartUtc = equipmentEvent.EventUtcTime,
            EndUtc = equipmentEvent.EventUtcTime.AddHours(2),
            TotalTripHours = 2
        };
        trip.AddTripEvent(equipmentEvent, 0);
        var tripEvent = new TripEvent
        {
            Id = 4,
            TripId = trip.Id,
            EquipmentEventId = equipmentEvent.Id,
            Sequence = 0,
            Trip = trip,
            EquipmentEvent = equipmentEvent
        };

        Assert.Equal("Alpha", city.Name);
        Assert.Equal("CAR1", equipmentEvent.EquipmentId);
        Assert.Equal(2, trip.TotalTripHours);
        Assert.Equal(trip.Id, tripEvent.TripId);
        Assert.Single(trip.TripEvents);
    }
}
