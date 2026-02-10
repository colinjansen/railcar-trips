using RailcarTrips.Domain.Models;

namespace RailcarTrips.Application.Abstractions;

public interface ITripProcessingStore
{
    Task<Dictionary<int, City>> GetCityLookupAsync(CancellationToken cancellationToken);
    Task<IReadOnlySet<EventKey>> GetExistingEventKeysAsync(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken);
    Task<PersistenceWriteResult> AddEquipmentEventsAsync(IEnumerable<EquipmentEvent> events, CancellationToken cancellationToken);
    Task<List<EquipmentEvent>> GetEventsForEquipmentAsync(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken);
    Task<IReadOnlySet<TripKey>> GetExistingTripKeysAsync(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken);
    Task<PersistenceWriteResult> AddTripsAsync(IEnumerable<Trip> trips, IEnumerable<TripEvent> tripEvents, CancellationToken cancellationToken);
}

public sealed record PersistenceWriteResult(
    int PersistedCount,
    IReadOnlyList<TripBuildWarning> Warnings);
