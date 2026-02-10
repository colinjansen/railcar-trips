using System.ComponentModel.DataAnnotations;

namespace RailcarTrips.Domain.Models;

public sealed class Trip
{
    private const string StartCode = "W";
    private const string EndCode = "Z";

    public int Id { get; set; }

    [Required]
    [MaxLength(32)]
    public string EquipmentId { get; set; } = string.Empty;

    public int OriginCityId { get; set; }
    public City? OriginCity { get; set; }

    public int DestinationCityId { get; set; }
    public City? DestinationCity { get; set; }

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public double TotalTripHours { get; set; }

    public List<TripEvent> TripEvents { get; set; } = [];

    public TripEvent AddTripEvent(EquipmentEvent equipmentEvent, int sequence)
    {
        var tripEvent = new TripEvent
        {
            Trip = this,
            EquipmentEventId = equipmentEvent.Id,
            Sequence = sequence
        };
        TripEvents.Add(tripEvent);
        return tripEvent;
    }

    public TripKey ToKey() => new(
        EquipmentId,
        DateTime.SpecifyKind(StartUtc, DateTimeKind.Utc),
        DateTime.SpecifyKind(EndUtc, DateTimeKind.Utc));

    public static TripBuildResult BuildTrips(IEnumerable<EquipmentEvent> events)
    {
        var warnings = new List<TripBuildWarning>();
        var trips = new List<Trip>();
        var tripEvents = new List<TripEvent>();

        var grouped = events
            .GroupBy(e => e.EquipmentId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in grouped)
        {
            var ordered = group.OrderBy(e => e.EventUtcTime).ToList();
            var currentStart = (EquipmentEvent?)null;
            var currentEvents = new List<EquipmentEvent>();

            foreach (var equipmentEvent in ordered)
            {
                if (equipmentEvent.EventCode.Equals(StartCode, StringComparison.OrdinalIgnoreCase))
                {
                    if (currentStart is not null)
                    {
                        warnings.Add(new TripBuildWarning("OverlappingStart",
                            $"Overlapping trip start for equipment {equipmentEvent.EquipmentId} at {equipmentEvent.EventUtcTime:u}",
                            equipmentEvent.EquipmentId,
                            equipmentEvent.EventUtcTime));
                        continue;
                    }

                    currentStart = equipmentEvent;
                    currentEvents.Clear();
                    currentEvents.Add(equipmentEvent);
                    continue;
                }

                if (currentStart is null)
                {
                    if (equipmentEvent.EventCode.Equals(EndCode, StringComparison.OrdinalIgnoreCase))
                    {
                        warnings.Add(new TripBuildWarning("EndWithoutStart",
                            $"Trip end without start for equipment {equipmentEvent.EquipmentId} at {equipmentEvent.EventUtcTime:u}",
                            equipmentEvent.EquipmentId,
                            equipmentEvent.EventUtcTime));
                    }

                    continue;
                }

                currentEvents.Add(equipmentEvent);

                if (equipmentEvent.EventCode.Equals(EndCode, StringComparison.OrdinalIgnoreCase))
                {
                    var trip = new Trip
                    {
                        EquipmentId = equipmentEvent.EquipmentId,
                        OriginCityId = currentStart.CityId,
                        DestinationCityId = equipmentEvent.CityId,
                        StartUtc = currentStart.EventUtcTime,
                        EndUtc = equipmentEvent.EventUtcTime,
                        TotalTripHours = (equipmentEvent.EventUtcTime - currentStart.EventUtcTime).TotalHours
                    };

                    trips.Add(trip);

                    var sequence = 0;
                    foreach (var tripEvent in currentEvents)
                    {
                        var newTripEvent = trip.AddTripEvent(tripEvent, sequence++);
                        tripEvents.Add(newTripEvent);
                    }

                    currentStart = null;
                    currentEvents.Clear();
                }
            }

            if (currentStart is not null)
            {
                warnings.Add(new TripBuildWarning("StartWithoutEnd",
                    $"Trip start without end for equipment {currentStart.EquipmentId} at {currentStart.EventUtcTime:u}",
                    currentStart.EquipmentId,
                    currentStart.EventUtcTime));
            }
        }

        return new TripBuildResult(trips, tripEvents, warnings);
    }
}
