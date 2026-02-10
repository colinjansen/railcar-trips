using RailcarTrips.Application.Abstractions;
using RailcarTrips.Shared.Dtos;

namespace RailcarTrips.Application.UseCases;

public sealed class TripQueryService(ITripReadStore store)
{
    private readonly ITripReadStore _store = store;

    /// <summary>
    /// Gets all trips.
    /// </summary>
    /// <param name="cancellationToken"> The cancellation token.</param>
    /// <returns> A list of trips.</returns>
    public Task<List<TripDto>> GetTrips(CancellationToken cancellationToken) =>
        _store.GetTrips(cancellationToken);

    /// <summary>
    /// Gets all events for a trip.
    /// </summary>
    /// <param name="tripId">The ID of the trip.</param>
    /// <param name="cancellationToken"> The cancellation token.</param>
    /// <returns> A list of trip events for the specified trip.</returns>
    public Task<List<TripEventDto>> GetTripEvents(int tripId, CancellationToken cancellationToken) =>
        _store.GetTripEvents(tripId, cancellationToken);
}
