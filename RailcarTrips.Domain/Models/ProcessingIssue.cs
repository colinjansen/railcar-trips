namespace RailcarTrips.Domain.Models;

public enum ProcessingIssueSeverity
{
    Warning,
    Error
}

public sealed record ProcessingIssue(
    string Code,
    string Message,
    ProcessingIssueSeverity Severity);
