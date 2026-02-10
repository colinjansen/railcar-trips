using Microsoft.EntityFrameworkCore;
using RailcarTrips.Application.Abstractions;
using RailcarTrips.Infrastructure.Data;
using RailcarTrips.Shared.Dtos;

namespace RailcarTrips.Infrastructure.Stores;

public sealed class TripReadStore(AppDbContext dbContext) : ITripReadStore
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<List<TripDto>> GetTripsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Trips
            .AsNoTracking()
            .Include(t => t.OriginCity)
            .Include(t => t.DestinationCity)
            .OrderByDescending(t => t.StartUtc)
            .Select(t => new TripDto
            {
                Id = t.Id,
                EquipmentId = t.EquipmentId,
                OriginCity = t.OriginCity != null ? t.OriginCity.Name : string.Empty,
                DestinationCity = t.DestinationCity != null ? t.DestinationCity.Name : string.Empty,
                StartUtc = t.StartUtc,
                EndUtc = t.EndUtc,
                TotalTripHours = t.TotalTripHours
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TripEventDto>> GetTripEventsAsync(int tripId, CancellationToken cancellationToken)
    {
        return await _dbContext.TripEvents
            .AsNoTracking()
            .Include(te => te.EquipmentEvent)
            .ThenInclude(e => e!.City)
            .Where(te => te.TripId == tripId)
            .OrderBy(te => te.Sequence)
            .Select(te => new TripEventDto
            {
                Id = te.EquipmentEvent!.Id,
                EquipmentId = te.EquipmentEvent!.EquipmentId,
                EventCode = te.EquipmentEvent!.EventCode,
                City = te.EquipmentEvent!.City != null ? te.EquipmentEvent!.City!.Name : string.Empty,
                EventLocalTime = te.EquipmentEvent!.EventLocalTime,
                EventUtcTime = te.EquipmentEvent!.EventUtcTime
            })
            .ToListAsync(cancellationToken);
    }
}
