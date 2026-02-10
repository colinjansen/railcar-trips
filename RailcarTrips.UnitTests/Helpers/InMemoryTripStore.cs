using RailcarTrips.Application.Abstractions;
using RailcarTrips.Domain.Models;

namespace RailcarTrips.UnitTests.Helpers;

public sealed class InMemoryTripStore : ITripProcessingStore
{
    public Dictionary<int, City> Cities { get; } = [];
    public List<EquipmentEvent> EquipmentEvents { get; } = [];
    public List<Trip> Trips { get; } = [];
    public List<TripEvent> TripEvents { get; } = [];

    public Task<Dictionary<int, City>> GetCityLookup(CancellationToken cancellationToken) =>
        Task.FromResult(Cities.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

    public Task<IReadOnlySet<EventKey>> GetExistingEventKeys(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken)
    {
        var keys = EquipmentEvents
            .Where(e => equipmentIds.Contains(e.EquipmentId))
            .Select(e => e.ToKey())
            .ToList();
        return Task.FromResult<IReadOnlySet<EventKey>>(new HashSet<EventKey>(keys));
    }

    public Task<PersistenceWriteResult> AddEquipmentEvents(IEnumerable<EquipmentEvent> events, CancellationToken cancellationToken)
    {
        var list = events.ToList();
        EquipmentEvents.AddRange(list);
        return Task.FromResult(new PersistenceWriteResult(list.Count, []));
    }

    public Task<List<EquipmentEvent>> GetEventsForEquipment(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken)
    {
        var events = EquipmentEvents
            .Where(e => equipmentIds.Contains(e.EquipmentId))
            .OrderBy(e => e.EquipmentId)
            .ThenBy(e => e.EventUtcTime)
            .ToList();
        return Task.FromResult(events);
    }

    public Task<IReadOnlySet<TripKey>> GetExistingTripKeys(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken)
    {
        var keys = Trips
            .Where(t => equipmentIds.Contains(t.EquipmentId))
            .Select(t => t.ToKey())
            .ToList();
        return Task.FromResult<IReadOnlySet<TripKey>>(new HashSet<TripKey>(keys));
    }

    public Task<PersistenceWriteResult> AddTrips(IEnumerable<Trip> trips, IEnumerable<TripEvent> tripEvents, CancellationToken cancellationToken)
    {
        var tripList = trips.ToList();
        Trips.AddRange(tripList);
        TripEvents.AddRange(tripEvents);
        return Task.FromResult(new PersistenceWriteResult(tripList.Count, []));
    }
}
