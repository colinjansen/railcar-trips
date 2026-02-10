namespace RailcarTrips.Domain.Models;

public readonly record struct ImportedEventRow(
    string EquipmentId,
    string EventCode,
    DateTime EventLocalTime,
    int CityId,
    string RawRow,
    int LineNumber);
