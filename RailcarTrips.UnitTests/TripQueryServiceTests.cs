using RailcarTrips.Application.Abstractions;
using RailcarTrips.Application.UseCases;
using RailcarTrips.Shared.Dtos;

namespace RailcarTrips.UnitTests;

public sealed class TripQueryServiceTests
{
    [Fact]
    public async Task GetTripsAsync_ReturnsTripsFromStore()
    {
        var store = new FakeTripReadStore
        {
            Trips =
            [
                new() { Id = 1, EquipmentId = "CAR1" },
                new() { Id = 2, EquipmentId = "CAR2" }
            ]
        };

        var service = new TripQueryService(store);

        var trips = await service.GetTrips(CancellationToken.None);

        Assert.Equal(2, trips.Count);
        Assert.Equal("CAR1", trips[0].EquipmentId);
    }

    [Fact]
    public async Task GetTripEventsAsync_ReturnsEventsFromStore()
    {
        var store = new FakeTripReadStore
        {
            TripEvents =
            [
                new() { Id = 10, EquipmentId = "CAR1", EventCode = "W" }
            ]
        };

        var service = new TripQueryService(store);

        var events = await service.GetTripEvents(99, CancellationToken.None);

        Assert.Single(events);
        Assert.Equal("W", events[0].EventCode);
    }

    private sealed class FakeTripReadStore : ITripReadStore
    {
        public List<TripDto> Trips { get; set; } = [];
        public List<TripEventDto> TripEvents { get; set; } = [];

        public Task<List<TripDto>> GetTrips(CancellationToken cancellationToken) =>
            Task.FromResult(Trips);

        public Task<List<TripEventDto>> GetTripEvents(int tripId, CancellationToken cancellationToken) =>
            Task.FromResult(TripEvents);
    }
}
