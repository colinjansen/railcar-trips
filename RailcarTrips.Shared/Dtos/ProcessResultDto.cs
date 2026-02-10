namespace RailcarTrips.Shared.Dtos;

public sealed class ProcessResultDto
{
    public int ParsedEvents { get; set; }
    public int StoredEvents { get; set; }
    public int TripsCreated { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
}
