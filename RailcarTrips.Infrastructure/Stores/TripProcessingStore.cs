using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RailcarTrips.Application.Abstractions;
using RailcarTrips.Domain.Models;
using RailcarTrips.Infrastructure.Data;

namespace RailcarTrips.Infrastructure.Stores;

public sealed class TripProcessingStore(AppDbContext dbContext) : ITripProcessingStore
{
    private readonly AppDbContext _dbContext = dbContext;

    public Task<Dictionary<int, City>> GetCityLookup(CancellationToken cancellationToken) =>
        _dbContext.Cities.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);

    public async Task<IReadOnlySet<EventKey>> GetExistingEventKeys(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken)
    {
        var keys = await _dbContext.EquipmentEvents
            .AsNoTracking()
            .Where(e => equipmentIds.Contains(e.EquipmentId))
            .Select(e => e.ToKey())
            .ToListAsync(cancellationToken);

        return new HashSet<EventKey>(keys);
    }

    public async Task<PersistenceWriteResult> AddEquipmentEvents(IEnumerable<EquipmentEvent> events, CancellationToken cancellationToken)
    {
        var pendingEvents = events.ToList();
        if (pendingEvents.Count == 0)
        {
            return new PersistenceWriteResult(0, []);
        }

        var warnings = new List<TripBuildWarning>();
        var persistedCount = 0;
        var equipmentIds = pendingEvents
            .Select(e => e.EquipmentId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        while (pendingEvents.Count > 0)
        {
            try
            {
                _dbContext.EquipmentEvents.AddRange(pendingEvents);
                await _dbContext.SaveChangesAsync(cancellationToken);
                persistedCount += pendingEvents.Count;
                pendingEvents.Clear();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _dbContext.ChangeTracker.Clear();
                var existingRows = await _dbContext.EquipmentEvents
                    .AsNoTracking()
                    .Where(e => equipmentIds.Contains(e.EquipmentId))
                    .Select(e => new
                    {
                        e.EquipmentId,
                        e.EventCode,
                        e.CityId,
                        UtcTicks = e.EventUtcTime.Ticks
                    })
                    .ToListAsync(cancellationToken);

                var remaining = new List<EquipmentEvent>();
                foreach (var pendingEvent in pendingEvents)
                {
                    var isDuplicate = existingRows.Any(existing =>
                        existing.CityId == pendingEvent.CityId &&
                        existing.UtcTicks == pendingEvent.EventUtcTime.Ticks &&
                        existing.EquipmentId.Equals(pendingEvent.EquipmentId, StringComparison.OrdinalIgnoreCase) &&
                        existing.EventCode.Equals(pendingEvent.EventCode, StringComparison.OrdinalIgnoreCase));

                    if (isDuplicate)
                    {
                        warnings.Add(new TripBuildWarning(
                            "DuplicateEvent",
                            $"Duplicate event for equipment {pendingEvent.EquipmentId} at {pendingEvent.EventUtcTime:u}",
                            pendingEvent.EquipmentId,
                            pendingEvent.EventUtcTime));
                        continue;
                    }

                    remaining.Add(pendingEvent);
                }

                if (remaining.Count == pendingEvents.Count)
                {
                    throw;
                }

                pendingEvents = remaining;
            }
        }

        return new PersistenceWriteResult(persistedCount, warnings);
    }

    public Task<List<EquipmentEvent>> GetEventsForEquipment(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken) =>
        _dbContext.EquipmentEvents
            .AsNoTracking()
            .Where(e => equipmentIds.Contains(e.EquipmentId))
            .OrderBy(e => e.EquipmentId)
            .ThenBy(e => e.EventUtcTime)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<TripKey>> GetExistingTripKeys(IReadOnlyCollection<string> equipmentIds, CancellationToken cancellationToken)
    {
        var keys = await _dbContext.Trips
            .AsNoTracking()
            .Where(t => equipmentIds.Contains(t.EquipmentId))
            .Select(t => t.ToKey())
            .ToListAsync(cancellationToken);

        return new HashSet<TripKey>(keys);
    }

    public async Task<PersistenceWriteResult> AddTrips(IEnumerable<Trip> trips, IEnumerable<TripEvent> tripEvents, CancellationToken cancellationToken)
    {
        var pendingTrips = trips.ToList();
        var pendingTripEvents = tripEvents.ToList();
        if (pendingTrips.Count == 0)
        {
            return new PersistenceWriteResult(0, []);
        }

        var warnings = new List<TripBuildWarning>();
        var persistedCount = 0;
        var equipmentIds = pendingTrips
            .Select(t => t.EquipmentId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        while (pendingTrips.Count > 0)
        {
            try
            {
                _dbContext.Trips.AddRange(pendingTrips);
                _dbContext.TripEvents.AddRange(pendingTripEvents);
                await _dbContext.SaveChangesAsync(cancellationToken);
                persistedCount += pendingTrips.Count;
                pendingTrips.Clear();
                pendingTripEvents.Clear();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _dbContext.ChangeTracker.Clear();
                var existingTrips = await _dbContext.Trips
                    .AsNoTracking()
                    .Where(t => equipmentIds.Contains(t.EquipmentId))
                    .Select(t => new
                    {
                        t.EquipmentId,
                        StartTicks = t.StartUtc.Ticks,
                        EndTicks = t.EndUtc.Ticks
                    })
                    .ToListAsync(cancellationToken);

                var remainingTrips = new List<Trip>();
                foreach (var trip in pendingTrips)
                {
                    var isDuplicate = existingTrips.Any(existing =>
                        existing.StartTicks == trip.StartUtc.Ticks &&
                        existing.EndTicks == trip.EndUtc.Ticks &&
                        existing.EquipmentId.Equals(trip.EquipmentId, StringComparison.OrdinalIgnoreCase));

                    if (isDuplicate)
                    {
                        warnings.Add(new TripBuildWarning(
                            "DuplicateTrip",
                            $"Duplicate trip for equipment {trip.EquipmentId} from {trip.StartUtc:u} to {trip.EndUtc:u}",
                            trip.EquipmentId,
                            trip.StartUtc));
                        continue;
                    }

                    remainingTrips.Add(trip);
                }

                if (remainingTrips.Count == pendingTrips.Count)
                {
                    throw;
                }

                pendingTrips = remainingTrips;
                var tripSet = pendingTrips.ToHashSet();
                pendingTripEvents = pendingTripEvents
                    .Where(te => te.Trip is not null && tripSet.Contains(te.Trip))
                    .ToList();
            }
        }

        return new PersistenceWriteResult(persistedCount, warnings);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqliteException { SqliteErrorCode: 19 };
}
