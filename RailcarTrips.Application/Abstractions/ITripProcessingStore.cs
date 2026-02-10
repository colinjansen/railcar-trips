using RailcarTrips.Domain.Models;

namespace RailcarTrips.Application.Abstractions;

public interface ITripProcessingStore
{
    Task<Dictionary<int, City>> GetCityLookupAsync(CancellationToken cancellationToken);
    Task<HashSet<EventKey>> GetExistingEventKeysAsync(HashSet<string> equipmentIds, CancellationToken cancellationToken);
    Task AddEquipmentEventsAsync(IEnumerable<EquipmentEvent> events, CancellationToken cancellationToken);
    Task<List<EquipmentEvent>> GetEventsForEquipmentAsync(string equipmentId, CancellationToken cancellationToken);
    Task<HashSet<TripKey>> GetExistingTripKeysAsync(HashSet<string> equipmentIds, CancellationToken cancellationToken);
    Task AddTripsAsync(IEnumerable<Trip> trips, IEnumerable<TripEvent> tripEvents, CancellationToken cancellationToken);
}
