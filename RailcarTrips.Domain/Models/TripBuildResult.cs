namespace RailcarTrips.Domain.Models;

public sealed record TripBuildResult(
    IReadOnlyList<Trip> Trips,
    IReadOnlyList<TripEvent> TripEvents,
    IReadOnlyList<TripBuildWarning> Warnings);
