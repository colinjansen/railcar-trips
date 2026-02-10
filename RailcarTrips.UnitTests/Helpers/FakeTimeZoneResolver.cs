using RailcarTrips.Application.Abstractions;

namespace RailcarTrips.UnitTests.Helpers;

public sealed class FakeTimeZoneResolver : ITimeZoneResolver
{
    private readonly Dictionary<string, TimeZoneInfo> _timeZones = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string id, TimeZoneInfo timeZone) => _timeZones[id] = timeZone;

    public TimeZoneInfo? Resolve(string timeZoneId) =>
        _timeZones.TryGetValue(timeZoneId, out var tz) ? tz : null;
}
