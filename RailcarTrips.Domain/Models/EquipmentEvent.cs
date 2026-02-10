using System.ComponentModel.DataAnnotations;

namespace RailcarTrips.Domain.Models;

public sealed class EquipmentEvent
{
    public int Id { get; set; }

    [Required]
    [MaxLength(32)]
    public string EquipmentId { get; set; } = string.Empty;

    [Required]
    [MaxLength(8)]
    public string EventCode { get; set; } = string.Empty;

    public DateTime EventLocalTime { get; set; }
    public DateTime EventUtcTime { get; set; }

    public int CityId { get; set; }
    public City? City { get; set; }

    public EventKey ToKey() => new(EquipmentId, EventUtcTime, EventCode, CityId);
}
