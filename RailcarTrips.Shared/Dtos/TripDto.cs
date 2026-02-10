namespace RailcarTrips.Shared.Dtos;

public sealed class TripDto
{
    public int Id { get; set; }
    public string EquipmentId { get; set; } = string.Empty;
    public string OriginCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public double TotalTripHours { get; set; }
}
