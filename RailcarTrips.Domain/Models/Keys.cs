namespace RailcarTrips.Domain.Models;

public readonly record struct EventKey(string EquipmentId, DateTime EventUtc, string EventCode, int CityId);
public readonly record struct TripKey(string EquipmentId, DateTime StartUtc, DateTime EndUtc);
