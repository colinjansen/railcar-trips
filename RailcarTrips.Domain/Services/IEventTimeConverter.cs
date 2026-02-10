namespace RailcarTrips.Domain.Services;

public interface IEventTimeConverter
{
    EventTimeConversionResult? Convert(DateTime eventLocalTime, string timeZoneId);
}

public sealed record EventTimeConversionResult(
    DateTime NormalizedLocalTime,
    DateTime EventUtcTime,
    bool InvalidLocalTimeAdjusted);
