namespace RailcarTrips.Shared.Dtos;

public sealed class TripEventDto
{
    public int Id { get; set; }
    public string EquipmentId { get; set; } = string.Empty;
    public string EventCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime EventLocalTime { get; set; }
    public DateTime EventUtcTime { get; set; }
}
