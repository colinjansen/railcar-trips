using RailcarTrips.Shared.Dtos;

namespace RailcarTrips.Application.Abstractions;

public interface ITripReadStore
{
    Task<List<TripDto>> GetTripsAsync(CancellationToken cancellationToken);
    Task<List<TripEventDto>> GetTripEventsAsync(int tripId, CancellationToken cancellationToken);
}
