namespace RailcarTrips.Domain.Models;

public sealed record TripBuildWarning(string Code, string Message, string EquipmentId, DateTime? EventUtc);
