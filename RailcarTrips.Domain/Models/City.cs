using System.ComponentModel.DataAnnotations;

namespace RailcarTrips.Domain.Models;

public sealed class City
{
    public int Id { get; set; }

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string TimeZoneId { get; set; } = string.Empty;
}
