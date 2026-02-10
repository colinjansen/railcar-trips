using Microsoft.Extensions.Logging;
using RailcarTrips.Domain.Services;
using TimeZoneConverter;

namespace RailcarTrips.Infrastructure.Services;

public sealed class TimeZoneResolver(ILogger<TimeZoneResolver> logger) : IEventTimeConverter
{
    private readonly ILogger<TimeZoneResolver> _logger = logger;

    public EventTimeConversionResult? Convert(DateTime eventLocalTime, string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        try
        {
            var timeZone = TZConvert.GetTimeZoneInfo(timeZoneId);
            var localTime = DateTime.SpecifyKind(eventLocalTime, DateTimeKind.Unspecified);
            var adjusted = false;

            if (timeZone.IsInvalidTime(localTime))
            {
                localTime = localTime.AddHours(1);
                adjusted = true;
            }

            var utcTime = TimeZoneInfo.ConvertTimeToUtc(localTime, timeZone);
            return new EventTimeConversionResult(localTime, utcTime, adjusted);
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogError("Time zone not found: {TimeZoneId}", timeZoneId);
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            _logger.LogError("Invalid time zone: {TimeZoneId}", timeZoneId);
            return null;
        }
    }
}
