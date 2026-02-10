namespace RailcarTrips.Domain.Models;

public sealed class TripEvent
{
    public int Id { get; set; }

    public int TripId { get; set; }
    public Trip? Trip { get; set; }

    public int EquipmentEventId { get; set; }
    public EquipmentEvent? EquipmentEvent { get; set; }

    public int Sequence { get; set; }
}
