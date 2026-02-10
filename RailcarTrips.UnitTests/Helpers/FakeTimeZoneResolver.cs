using RailcarTrips.Domain.Services;

namespace RailcarTrips.UnitTests.Helpers;

public sealed class FakeTimeZoneResolver : IEventTimeConverter
{
    private readonly Dictionary<string, TimeZoneInfo> _timeZones = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string id, TimeZoneInfo timeZone) => _timeZones[id] = timeZone;

    public EventTimeConversionResult? Convert(DateTime eventLocalTime, string timeZoneId)
    {
        if (!_timeZones.TryGetValue(timeZoneId, out var tz))
        {
            return null;
        }

        var localTime = DateTime.SpecifyKind(eventLocalTime, DateTimeKind.Unspecified);
        var adjusted = false;
        if (tz.IsInvalidTime(localTime))
        {
            localTime = localTime.AddHours(1);
            adjusted = true;
        }

        var utcTime = TimeZoneInfo.ConvertTimeToUtc(localTime, tz);
        return new EventTimeConversionResult(localTime, utcTime, adjusted);
    }
}
