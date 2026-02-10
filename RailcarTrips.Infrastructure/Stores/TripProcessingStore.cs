using Microsoft.EntityFrameworkCore;
using RailcarTrips.Application.Abstractions;
using RailcarTrips.Domain.Models;
using RailcarTrips.Infrastructure.Data;

namespace RailcarTrips.Infrastructure.Stores;

public sealed class TripProcessingStore(AppDbContext dbContext) : ITripProcessingStore
{
    private readonly AppDbContext _dbContext = dbContext;

    public Task<Dictionary<int, City>> GetCityLookupAsync(CancellationToken cancellationToken) =>
        _dbContext.Cities.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);

    public async Task<HashSet<EventKey>> GetExistingEventKeysAsync(HashSet<string> equipmentIds, CancellationToken cancellationToken)
    {
        var keys = await _dbContext.EquipmentEvents
            .AsNoTracking()
            .Where(e => equipmentIds.Contains(e.EquipmentId))
            .Select(e => e.ToKey())
            .ToListAsync(cancellationToken);

        return new HashSet<EventKey>(keys);
    }

    public async Task AddEquipmentEventsAsync(IEnumerable<EquipmentEvent> events, CancellationToken cancellationToken)
    {
        _dbContext.EquipmentEvents.AddRange(events);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<EquipmentEvent>> GetEventsForEquipmentAsync(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken) =>
        _dbContext.EquipmentEvents
            .AsNoTracking()
            .Where(e => equipmentIds.Contains(e.EquipmentId))
            .OrderBy(e => e.EquipmentId)
            .ThenBy(e => e.EventUtcTime)
            .ToListAsync(cancellationToken);

    public async Task<HashSet<TripKey>> GetExistingTripKeysAsync(HashSet<string> equipmentIds, CancellationToken cancellationToken)
    {
        var keys = await _dbContext.Trips
            .AsNoTracking()
            .Where(t => equipmentIds.Contains(t.EquipmentId))
            .Select(t => t.ToKey())
            .ToListAsync(cancellationToken);

        return new HashSet<TripKey>(keys);
    }

    public async Task AddTripsAsync(IEnumerable<Trip> trips, IEnumerable<TripEvent> tripEvents, CancellationToken cancellationToken)
    {
        _dbContext.Trips.AddRange(trips);
        _dbContext.TripEvents.AddRange(tripEvents);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
