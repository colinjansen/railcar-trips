namespace RailcarTrips.Application.Abstractions;

public interface ITimeZoneResolver
{
    TimeZoneInfo? Resolve(string timeZoneId);
}
