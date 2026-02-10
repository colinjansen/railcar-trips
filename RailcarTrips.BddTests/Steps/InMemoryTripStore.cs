using RailcarTrips.Application.Abstractions;
using RailcarTrips.Domain.Models;
using RailcarTrips.Shared.Dtos;

namespace RailcarTrips.BddTests.Steps;

public sealed class InMemoryTripStore : ITripProcessingStore, ITripReadStore
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

    public Task<List<TripDto>> GetTrips(CancellationToken cancellationToken)
    {
        var trips = Trips
            .OrderByDescending(t => t.StartUtc)
            .Select(t => new TripDto
            {
                Id = t.Id,
                EquipmentId = t.EquipmentId,
                OriginCity = Cities.TryGetValue(t.OriginCityId, out var origin) ? origin.Name : string.Empty,
                DestinationCity = Cities.TryGetValue(t.DestinationCityId, out var dest) ? dest.Name : string.Empty,
                StartUtc = t.StartUtc,
                EndUtc = t.EndUtc,
                TotalTripHours = t.TotalTripHours
            })
            .ToList();
        return Task.FromResult(trips);
    }

    public Task<List<TripEventDto>> GetTripEvents(int tripId, CancellationToken cancellationToken)
    {
        var events = TripEvents
            .Where(te => te.TripId == tripId)
            .OrderBy(te => te.Sequence)
            .Select(te =>
            {
                var e = EquipmentEvents.FirstOrDefault(x => x.Id == te.EquipmentEventId);
                return new TripEventDto
                {
                    Id = e?.Id ?? 0,
                    EquipmentId = e?.EquipmentId ?? string.Empty,
                    EventCode = e?.EventCode ?? string.Empty,
                    City = e != null && Cities.TryGetValue(e.CityId, out var city) ? city.Name : string.Empty,
                    EventLocalTime = e?.EventLocalTime ?? default,
                    EventUtcTime = e?.EventUtcTime ?? default
                };
            })
            .ToList();

        return Task.FromResult(events);
    }
}
