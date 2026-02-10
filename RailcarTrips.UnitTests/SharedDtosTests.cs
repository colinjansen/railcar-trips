using RailcarTrips.Shared.Dtos;

namespace RailcarTrips.UnitTests;

public sealed class SharedDtosTests
{
    [Fact]
    public void TripDto_CanSetAndReadProperties()
    {
        var dto = new TripDto
        {
            Id = 1,
            EquipmentId = "CAR1",
            OriginCity = "Alpha",
            DestinationCity = "Beta",
            StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            TotalTripHours = 24
        };

        Assert.Equal("CAR1", dto.EquipmentId);
        Assert.Equal("Alpha", dto.OriginCity);
        Assert.Equal(24, dto.TotalTripHours);
    }

    [Fact]
    public void TripEventDto_CanSetAndReadProperties()
    {
        var dto = new TripEventDto
        {
            Id = 10,
            EquipmentId = "CAR1",
            EventCode = "W",
            City = "Alpha",
            EventLocalTime = new DateTime(2026, 1, 1, 0, 0, 0),
            EventUtcTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        Assert.Equal("W", dto.EventCode);
        Assert.Equal("Alpha", dto.City);
    }

    [Fact]
    public void ProcessResultDto_DefaultsToZero()
    {
        var dto = new ProcessResultDto();

        Assert.Equal(0, dto.ParsedEvents);
        Assert.Equal(0, dto.StoredEvents);
        Assert.Equal(0, dto.TripsCreated);
        Assert.Equal(0, dto.WarningCount);
        Assert.Equal(0, dto.ErrorCount);
    }
}
