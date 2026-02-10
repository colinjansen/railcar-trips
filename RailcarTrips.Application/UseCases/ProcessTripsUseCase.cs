using System.Globalization;
using Microsoft.Extensions.Logging;
using RailcarTrips.Application.Abstractions;
using RailcarTrips.Domain.Models;
using RailcarTrips.Domain.Services;
using RailcarTrips.Shared.Dtos;

namespace RailcarTrips.Application.UseCases;

public sealed class ProcessTripsUseCase(
    ITripProcessingStore store,
    ITimeZoneResolver timeZoneResolver,
    ICsvReader csvReader,
    ILogger<ProcessTripsUseCase> logger)
{
    private readonly ITripProcessingStore _store = store;
    private readonly ITimeZoneResolver _timeZoneResolver = timeZoneResolver;
    private readonly ICsvReader _csvReader = csvReader;
    private readonly ILogger<ProcessTripsUseCase> _logger = logger;

    /// <summary>
    /// Processes the provided CSV stream of equipment events, applying business rules to determine which events and trips to persist.
    /// </summary>
    /// <param name="csvStream">The stream containing CSV data of equipment events.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result object containing counts of processed, stored, and created entities.</returns>
    public async Task<ProcessResultDto> Execute(Stream csvStream, CancellationToken cancellationToken = default)
    {
        var result = new ProcessResultDto();
        var rows = await ReadRows(csvStream, result, cancellationToken);
        result.ParsedEvents = rows.Count;

        if (rows.Count == 0)
        {
            _logger.LogWarning("No events found to process.");
            return result;
        }

        var cityLookup = await _store.GetCityLookupAsync(cancellationToken);
        var buildEventsResult = TripProcessingRules.BuildEvents(rows, cityLookup, _timeZoneResolver.Resolve);
        LogAndCountIssues(buildEventsResult.Issues, result);
        var eventsToInsert = buildEventsResult.Events;
        var equipmentIds = buildEventsResult.EquipmentIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (eventsToInsert.Count == 0)
        {
            _logger.LogWarning("No valid events to insert.");
            return result;
        }

        var existingEventKeys = await _store.GetExistingEventKeysAsync(equipmentIds, cancellationToken);
        var eventSelection = TripProcessingRules.SelectEventsToPersist(eventsToInsert, existingEventKeys);
        LogAndCountWarnings(eventSelection.Warnings, result);
        result.StoredEvents += eventSelection.Events.Count;

        if (eventSelection.Events.Count > 0)
        {
            await _store.AddEquipmentEventsAsync(eventSelection.Events, cancellationToken);
        }

        var eventsForTrips = await LoadEventsForTrips(equipmentIds, cancellationToken);
        var existingTripKeys = await _store.GetExistingTripKeysAsync(equipmentIds, cancellationToken);
        var tripSelection = TripProcessingRules.SelectTripsToPersist(eventsForTrips, existingTripKeys);
        LogAndCountWarnings(tripSelection.Warnings, result);

        if (tripSelection.Trips.Count > 0)
        {
            await _store.AddTripsAsync(tripSelection.Trips, tripSelection.TripEvents, cancellationToken);
            result.TripsCreated = tripSelection.Trips.Count;
        }

        _logger.LogInformation("Processed {Parsed} events, stored {Stored} new events, created {Trips} trips.",
            result.ParsedEvents, result.StoredEvents, result.TripsCreated);

        return result;
    }

    /// <summary>
    /// Loads all events for the given equipment IDs from the database. This is necessary to ensure that we have a complete set 
    /// of events for building trips, including any existing events that were not part of the current CSV upload.
    /// </summary>
    /// <param name="equipmentIds">The set of equipment IDs for which to load events.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A list of <see cref="EquipmentEvent"/> objects for the specified equipment IDs.</returns>
    private async Task<List<EquipmentEvent>> LoadEventsForTrips(HashSet<string> equipmentIds, CancellationToken cancellationToken)
    {
        var eventsForTrips = new List<EquipmentEvent>();
        foreach (var equipmentId in equipmentIds)
        {
            var equipmentEvents = await _store.GetEventsForEquipmentAsync(equipmentId, cancellationToken);
            eventsForTrips.AddRange(equipmentEvents);
        }

        return eventsForTrips;
    }

    /// <summary>
    /// Logs the provided warnings and increments the warning count in the result. Warnings with codes starting 
    /// with "Duplicate" are logged at the Information level, while all others are logged at the Warning level.
    /// </summary>
    /// <param name="warnings">The collection of warnings to log and count.</param>
    /// <param name="result">The result object to update with warning counts.</param>
    private void LogAndCountWarnings(IEnumerable<TripBuildWarning> warnings, ProcessResultDto result)
    {
        foreach (var warning in warnings)
        {
            if (warning.Code.StartsWith("Duplicate", StringComparison.Ordinal))
            {
                _logger.LogInformation("{Code}: {Message}", warning.Code, warning.Message);
            }
            else
            {
                _logger.LogWarning("{Code}: {Message}", warning.Code, warning.Message);
            }

            result.WarningCount++;
        }
    }

    /// <summary>
    /// Logs the provided processing issues and increments the error and warning counts in the result accordingly. 
    /// Issues with severity "Error" are logged at the Error level, while those with severity "Warning" are logged at the Warning level.
    /// </summary>
    /// <param name="issues">The collection of processing issues to log and count.</param>
    /// <param name="result">The result object to update with error and warning counts.</param>
    private void LogAndCountIssues(IEnumerable<ProcessingIssue> issues, ProcessResultDto result)
    {
        foreach (var issue in issues)
        {
            if (issue.Severity == ProcessingIssueSeverity.Error)
            {
                _logger.LogError("{Code}: {Message}", issue.Code, issue.Message);
                result.ErrorCount++;
            }
            else
            {
                _logger.LogWarning("{Code}: {Message}", issue.Code, issue.Message);
                result.WarningCount++;
            }
        }
    }

    /// <summary>
    /// Reads and parses the CSV rows from the provided stream. Validates the format and logs any issues encountered.
    /// </summary>
    /// <param name="csvStream"><see cref="Stream"/> containing the CSV data.</param>
    /// <param name="result"><see cref="ProcessResultDto"/> to accumulate processing results.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A list of parsed CSV event rows.</returns>
    private async Task<List<ImportedEventRow>> ReadRows(Stream csvStream, ProcessResultDto result, CancellationToken cancellationToken)
    {
        var rows = new List<ImportedEventRow>();
        var csvRows = await _csvReader.ReadRows(stream: csvStream, skipHeaderRow: true, leaveStreamOpen: true, cancellationToken);

        foreach (string[] parts in csvRows)
        {
            var line = string.Join(',', parts);

            if (parts.Length < 4)
            {
                _logger.LogWarning("Skipping invalid row: {Row}", line);
                result.WarningCount++;
                continue;
            }

            if (!DateTime.TryParseExact(parts[2], "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var localTime))
            {
                _logger.LogWarning("Skipping row with invalid date: {Row}", line);
                result.WarningCount++;
                continue;
            }

            if (!int.TryParse(parts[3], out var cityId))
            {
                _logger.LogWarning("Skipping row with invalid city id: {Row}", line);
                result.WarningCount++;
                continue;
            }

            rows.Add(new ImportedEventRow(
                parts[0],
                parts[1],
                localTime,
                cityId,
                line,
                0));
        }

        return rows;
    }
}
