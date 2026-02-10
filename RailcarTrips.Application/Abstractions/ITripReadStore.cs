using RailcarTrips.Shared.Dtos;

namespace RailcarTrips.Application.Abstractions;

public interface ITripReadStore
{
    /// <summary>
    /// Gets a list of trips from the store. This is used to retrieve existing trips for display and analysis.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of trips.</returns>
    Task<List<TripDto>> GetTrips(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a list of trip events for a specific trip from the store. This is used to retrieve existing trip events for display and analysis.
    /// </summary>
    /// <param name="tripId">The ID of the trip to retrieve events for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of trip events for the specified trip.</returns>
    Task<List<TripEventDto>> GetTripEvents(int tripId, CancellationToken cancellationToken);
}
