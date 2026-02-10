using RailcarTrips.Domain.Models;

namespace RailcarTrips.Application.Abstractions;

public interface ITripProcessingStore
{
    /// <summary>
    /// Gets a lookup of city IDs to city information. This is used to enrich trip data with city information.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A dictionary mapping city IDs to city information.</returns>
    Task<Dictionary<int, City>> GetCityLookup(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a set of existing event keys for the given equipment IDs. This is used to avoid processing duplicate events.
    /// </summary>
    /// <param name="equipmentIds">The collection of equipment IDs to check for existing event keys.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A set of existing event keys for the specified equipment IDs.</returns>
    Task<IReadOnlySet<EventKey>> GetExistingEventKeys(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a collection of equipment events to the store. This is used to persist new events that have been processed.
    /// </summary>
    /// <param name="events">The collection of equipment events to add.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result indicating the outcome of the persistence operation.</returns>
    Task<PersistenceWriteResult> AddEquipmentEvents(IEnumerable<EquipmentEvent> events, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a list of equipment events for the specified equipment IDs. This is used to retrieve existing events for enrichment and processing.
    /// </summary>
    /// <param name="equipmentIds">The collection of equipment IDs to retrieve events for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of equipment events for the specified equipment IDs.</returns>
    Task<List<EquipmentEvent>> GetEventsForEquipment(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a set of existing trip keys for the given equipment IDs. This is used to avoid processing duplicate trips.
    /// </summary>
    /// <param name="equipmentIds">The collection of equipment IDs to check for existing trip keys.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A set of existing trip keys for the specified equipment IDs.</returns>
    Task<IReadOnlySet<TripKey>> GetExistingTripKeys(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a collection of trips and their associated events to the store. This is used to persist new trips that have been processed, along with any warnings that were generated during trip building.
    /// </summary>
    /// <param name="trips">The collection of trips to add.</param>
    /// <param name="tripEvents">The collection of trip events associated with the trips.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result indicating the outcome of the persistence operation.</returns>
    Task<PersistenceWriteResult> AddTrips(IEnumerable<Trip> trips, IEnumerable<TripEvent> tripEvents, CancellationToken cancellationToken);
}

public sealed record PersistenceWriteResult(
    int PersistedCount,
    IReadOnlyList<TripBuildWarning> Warnings);
