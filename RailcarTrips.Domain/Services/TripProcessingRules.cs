using RailcarTrips.Domain.Models;

namespace RailcarTrips.Domain.Services;

public static class TripProcessingRules
{
    public static EventBuildResult BuildEvents(
        IEnumerable<ImportedEventRow> rows,
        IReadOnlyDictionary<int, City> cityLookup,
        Func<string, TimeZoneInfo?> resolveTimeZone)
    {
        var eventsToInsert = new List<EquipmentEvent>();
        var equipmentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var issues = new List<ProcessingIssue>();

        foreach (var row in rows)
        {
            if (!cityLookup.TryGetValue(row.CityId, out var city))
            {
                issues.Add(new ProcessingIssue(
                    "UnknownCity",
                    $"Unknown city id {row.CityId} in row {row.RawRow}",
                    ProcessingIssueSeverity.Error));
                continue;
            }

            var timeZone = resolveTimeZone(city.TimeZoneId);
            if (timeZone is null)
            {
                issues.Add(new ProcessingIssue(
                    "TimeZoneNotFound",
                    $"Unable to resolve time zone for city {city.Id} {city.Name}",
                    ProcessingIssueSeverity.Error));
                continue;
            }

            var localTime = DateTime.SpecifyKind(row.EventLocalTime, DateTimeKind.Unspecified);
            if (timeZone.IsInvalidTime(localTime))
            {
                var adjusted = localTime.AddHours(1);
                issues.Add(new ProcessingIssue(
                    "InvalidLocalTimeAdjusted",
                    $"Invalid local time {localTime} in {city.TimeZoneId}. Adjusted to {adjusted}.",
                    ProcessingIssueSeverity.Warning));
                localTime = adjusted;
            }

            var utcTime = TimeZoneInfo.ConvertTimeToUtc(localTime, timeZone);
            var equipmentEvent = new EquipmentEvent
            {
                EquipmentId = row.EquipmentId,
                EventCode = row.EventCode,
                EventLocalTime = localTime,
                EventUtcTime = utcTime,
                CityId = city.Id
            };

            eventsToInsert.Add(equipmentEvent);
            equipmentIds.Add(equipmentEvent.EquipmentId);
        }

        return new EventBuildResult(eventsToInsert, equipmentIds, issues);
    }

    public static EventPersistenceSelection SelectEventsToPersist(
        IEnumerable<EquipmentEvent> candidateEvents,
        ISet<EventKey> existingKeys)
    {
        var newEvents = new List<EquipmentEvent>();
        var warnings = new List<TripBuildWarning>();
        var seenKeys = new HashSet<EventKey>(existingKeys);

        foreach (var candidateEvent in candidateEvents)
        {
            var key = candidateEvent.ToKey();
            if (seenKeys.Contains(key))
            {
                warnings.Add(new TripBuildWarning(
                    "DuplicateEvent",
                    $"Duplicate event for equipment {candidateEvent.EquipmentId} at {candidateEvent.EventUtcTime:u}",
                    candidateEvent.EquipmentId,
                    candidateEvent.EventUtcTime));
                continue;
            }

            newEvents.Add(candidateEvent);
            seenKeys.Add(key);
        }

        return new EventPersistenceSelection(newEvents, warnings);
    }

    public static TripPersistenceSelection SelectTripsToPersist(
        IEnumerable<EquipmentEvent> eventsForTripBuild,
        ISet<TripKey> existingTripKeys)
    {
        var buildResult = Trip.BuildTrips(eventsForTripBuild);
        var newTrips = new List<Trip>();
        var newTripEvents = new List<TripEvent>();
        var newTripSet = new HashSet<Trip>();
        var warnings = buildResult.Warnings.ToList();

        foreach (var trip in buildResult.Trips)
        {
            var key = trip.ToKey();
            if (existingTripKeys.Contains(key))
            {
                warnings.Add(new TripBuildWarning(
                    "DuplicateTrip",
                    $"Duplicate trip for equipment {trip.EquipmentId} from {trip.StartUtc:u} to {trip.EndUtc:u}",
                    trip.EquipmentId,
                    trip.StartUtc));
                continue;
            }

            newTrips.Add(trip);
            newTripSet.Add(trip);
        }

        foreach (var tripEvent in buildResult.TripEvents)
        {
            if (tripEvent.Trip is not null && newTripSet.Contains(tripEvent.Trip))
            {
                newTripEvents.Add(tripEvent);
            }
        }

        return new TripPersistenceSelection(newTrips, newTripEvents, warnings);
    }
}

public sealed record EventPersistenceSelection(
    IReadOnlyList<EquipmentEvent> Events,
    IReadOnlyList<TripBuildWarning> Warnings);

public sealed record EventBuildResult(
    IReadOnlyList<EquipmentEvent> Events,
    IReadOnlySet<string> EquipmentIds,
    IReadOnlyList<ProcessingIssue> Issues);

public sealed record TripPersistenceSelection(
    IReadOnlyList<Trip> Trips,
    IReadOnlyList<TripEvent> TripEvents,
    IReadOnlyList<TripBuildWarning> Warnings);
