using Microsoft.Extensions.Logging;
using RailcarTrips.Application.Abstractions;
using TimeZoneConverter;

namespace RailcarTrips.Infrastructure.Services;

public sealed class TimeZoneResolver(ILogger<TimeZoneResolver> logger) : ITimeZoneResolver
{
    private readonly ILogger<TimeZoneResolver> _logger = logger;

    public TimeZoneInfo? Resolve(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        try
        {
            return TZConvert.GetTimeZoneInfo(timeZoneId);
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
