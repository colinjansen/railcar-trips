using RailcarTrips.Application.Abstractions;
using RailcarTrips.Domain.Models;

namespace RailcarTrips.UnitTests.Helpers;

public sealed class InMemoryTripStore : ITripProcessingStore
{
    public Dictionary<int, City> Cities { get; } = new();
    public List<EquipmentEvent> EquipmentEvents { get; } = new();
    public List<Trip> Trips { get; } = new();
    public List<TripEvent> TripEvents { get; } = new();

    public Task<Dictionary<int, City>> GetCityLookupAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Cities.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

    public Task<IReadOnlySet<EventKey>> GetExistingEventKeysAsync(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken)
    {
        var keys = EquipmentEvents
            .Where(e => equipmentIds.Contains(e.EquipmentId))
            .Select(e => e.ToKey())
            .ToList();
        return Task.FromResult<IReadOnlySet<EventKey>>(new HashSet<EventKey>(keys));
    }

    public Task<PersistenceWriteResult> AddEquipmentEventsAsync(IEnumerable<EquipmentEvent> events, CancellationToken cancellationToken)
    {
        var list = events.ToList();
        EquipmentEvents.AddRange(list);
        return Task.FromResult(new PersistenceWriteResult(list.Count, []));
    }

    public Task<List<EquipmentEvent>> GetEventsForEquipmentAsync(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken)
    {
        var events = EquipmentEvents
            .Where(e => equipmentIds.Contains(e.EquipmentId))
            .OrderBy(e => e.EquipmentId)
            .ThenBy(e => e.EventUtcTime)
            .ToList();
        return Task.FromResult(events);
    }

    public Task<IReadOnlySet<TripKey>> GetExistingTripKeysAsync(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken)
    {
        var keys = Trips
            .Where(t => equipmentIds.Contains(t.EquipmentId))
            .Select(t => t.ToKey())
            .ToList();
        return Task.FromResult<IReadOnlySet<TripKey>>(new HashSet<TripKey>(keys));
    }

    public Task<PersistenceWriteResult> AddTripsAsync(IEnumerable<Trip> trips, IEnumerable<TripEvent> tripEvents, CancellationToken cancellationToken)
    {
        var tripList = trips.ToList();
        Trips.AddRange(tripList);
        TripEvents.AddRange(tripEvents);
        return Task.FromResult(new PersistenceWriteResult(tripList.Count, []));
    }
}
